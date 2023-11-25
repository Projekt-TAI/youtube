using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using TAIBackend.Utilities;
using Microsoft.AspNetCore.Http.Features;
using System.Text;
using System.Security.Claims;
using Newtonsoft.Json;

namespace TAIBackend.routes.streaming;

[Route("videos")]
public class StreamingController : Controller
{
    private class VideoDescriptor{
        [JsonProperty("id")]
        public int? Id;
        [JsonProperty("title")]
        public string? Title;
    }


    private readonly long _fileSizeLimit;
    private readonly ILogger<StreamingController> _logger;
    private readonly string[] _permittedExtensions = { ".webm" };
    private readonly string? _targetFilePath;

    private readonly string? _ffmpegFilePath;

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

        _ffmpegFilePath = config.GetValue<string>("FFMpegPath");
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("upload"), HttpPut("upload")]
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

        if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
        {
            return StatusCode(500);
        }
        var userDirectory = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(User.FindFirst(ClaimTypes.GivenName)!.Value));
        if (_targetFilePath == null || userDirectory == null)
        {
            return StatusCode(500);
        }

        var userPath = Path.Combine(_targetFilePath, userDirectory);
        Directory.CreateDirectory(userPath);

        if (section == null || section!.ContentType != "application/json")
        {
            return BadRequest();
        }

        var json = await section.ReadAsStringAsync();
        if (json == null)
        {
            return BadRequest();
        }
        VideoDescriptor? videoDescriptor = JsonConvert.DeserializeObject<VideoDescriptor>(json);
        if (videoDescriptor == null)
        {
            return BadRequest();
        }
        if (videoDescriptor.Id == null)
        {
            return BadRequest("Video id is required");
        }

        var trustedFileNameForFileStorage = videoDescriptor.Id;
        if(HttpContext.Request.Method == "POST"){
            if(System.IO.File.Exists(Path.Combine(userPath, $"{trustedFileNameForFileStorage}.json")) || System.IO.File.Exists(Path.Combine(userPath, $"{trustedFileNameForFileStorage}.webm"))){
                return Conflict("The file already exists. If you want to update the content, use HTTP PUT");
            }
        }

        using (var targetStream = System.IO.File.Create(
            Path.Combine(userPath, $"{trustedFileNameForFileStorage}.json")))
        {
            await targetStream.WriteAsync(Encoding.ASCII.GetBytes(json));

            _logger.LogInformation(
                "Uploaded file config, saved to " +
                "'{TargetFilePath}' as {TrustedFileNameForFileStorage}.json",
                _targetFilePath,
                trustedFileNameForFileStorage);
        }

        section = await reader.ReadNextSectionAsync();
        if (section == null)
        {
            return BadRequest();
        }
        var hasContentDispositionHeader =
            ContentDispositionHeaderValue.TryParse(
                section.ContentDisposition, out var contentDisposition);

        if (!hasContentDispositionHeader)
        {
            return BadRequest();
        }

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

            // **WARNING!**
            // In the following example, the file is saved without
            // scanning the file's contents. In most production
            // scenarios, an anti-virus/anti-malware scanner API
            // is used on the file before making the file available
            // for download or for use by other systems. 
            // For more information, see the topic that accompanies 
            // this sample.

            var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                section, contentDisposition!, ModelState,
                _permittedExtensions, _fileSizeLimit);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var targetStream = System.IO.File.Create(
                Path.Combine(userPath, $"{trustedFileNameForFileStorage}.webm")))
            {
                await targetStream.WriteAsync(streamedFileContent);

                _logger.LogInformation(
                    "Uploaded file saved to " +
                    "'{TargetFilePath}' as {TrustedFileNameForFileStorage}.webm",
                    _targetFilePath,
                    trustedFileNameForFileStorage);
            }
        }


        return Created(nameof(StreamingController), null);
    }
}