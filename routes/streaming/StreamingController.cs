using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using TAIBackend.Utilities;
using SampleApp.Utilities;
using Microsoft.AspNetCore.Http.Features;

namespace TAIBackend.routes.streaming;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class StreamingController : Controller
{

    private readonly long _fileSizeLimit;
    private readonly ILogger<StreamingController> _logger;
    private readonly string[] _permittedExtensions = { ".zip" };
    private readonly string? _targetFilePath;

    private static readonly FormOptions _defaultFormOptions = new FormOptions();

    public StreamingController(ILogger<StreamingController> logger,
         IConfiguration config)
    {
        _logger = logger;
        _fileSizeLimit = config.GetValue<long>("FileSizeLimit");

        // To save physical files to a path provided by configuration:
        _targetFilePath = config.GetValue<string>("StoredFilesPath");

        // To save physical files to the temporary files folder, use:
        //_targetFilePath = Path.GetTempPath();
    }

    [HttpPost("upload")]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> UploadPhysical()
    {
        if (Request.ContentType == null)
        {
            return BadRequest();
        }

        if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
        {
            ModelState.AddModelError("File",
                $"The request couldn't be processed (Error 1).");
            // Log error

            return BadRequest(ModelState);
        }

        var boundary = MultipartRequestHelper.GetBoundary(
            MediaTypeHeaderValue.Parse(Request.ContentType),
            _defaultFormOptions.MultipartBoundaryLengthLimit);
        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        var section = await reader.ReadNextSectionAsync();

        while (section != null)
        {
            if (section.ContentType=="application/json") {
                Console.WriteLine(section.ToString());
                continue;
            }
            var hasContentDispositionHeader =
                ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition);

            if (hasContentDispositionHeader)
            {
                // This check assumes that there's a file
                // present without form data. If form data
                // is present, this method immediately fails
                // and returns the model error.
                if (!MultipartRequestHelper
                    .HasFileContentDisposition(contentDisposition!))
                {
                    ModelState.AddModelError("File",
                        $"The request couldn't be processed (Error 2).");
                    // Log error

                    return BadRequest(ModelState);
                }
                else
                {
                    // Don't trust the file name sent by the client. To display
                    // the file name, HTML-encode the value.
                    var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                            contentDisposition!.FileName.Value);
                    var trustedFileNameForFileStorage = Path.GetRandomFileName();

                    // **WARNING!**
                    // In the following example, the file is saved without
                    // scanning the file's contents. In most production
                    // scenarios, an anti-virus/anti-malware scanner API
                    // is used on the file before making the file available
                    // for download or for use by other systems. 
                    // For more information, see the topic that accompanies 
                    // this sample.

                    var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                        section, contentDisposition, ModelState,
                        _permittedExtensions, _fileSizeLimit);

                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    if (_targetFilePath == null)
                    {
                        return StatusCode(500);
                    }
                    using (var targetStream = System.IO.File.Create(
                        Path.Combine(_targetFilePath, trustedFileNameForFileStorage)))
                    {
                        await targetStream.WriteAsync(streamedFileContent);

                        _logger.LogInformation(
                            "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                            "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                            trustedFileNameForDisplay, _targetFilePath,
                            trustedFileNameForFileStorage);
                    }
                }
            }

            // Drain any remaining section body that hasn't been consumed and
            // read the headers for the next section.
            section = await reader.ReadNextSectionAsync();
        }

        return Created(nameof(StreamingController), null);
    }
}