using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using TAIBackend.Utilities;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;

namespace TAIBackend.routes.streaming;

[Route("videos")]
public class StreamingController : Controller
{
    private readonly long _fileSizeLimit;
    private readonly ILogger<StreamingController> _logger;
    private readonly string[] _permittedExtensions = { ".mp4" };
    private readonly string _targetFilePath;

    private readonly string _mp4DashPath;
    private readonly string _mp4FragmentPath;

    private static readonly FormOptions _defaultFormOptions = new FormOptions();

    public StreamingController(ILogger<StreamingController> logger,
         IConfiguration config)
    {
        _logger = logger;
        _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
        _targetFilePath = config.GetValue<string>("StoredFilesPath") ?? throw new InvalidOperationException();;
        _mp4DashPath = config.GetValue<string>("MP4DashPath") ?? throw new InvalidOperationException();;
        _mp4FragmentPath = config.GetValue<string>("MP4FragmentPath") ?? throw new InvalidOperationException();;
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("upload/{id}"), HttpPut("upload/{id}")]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> UploadPhysical(uint id)
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

        if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
        {
            return StatusCode(500);
        }

        var userDirectory = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        if (_targetFilePath == null || userDirectory == null)
        {
            return StatusCode(500);
        }

        var userPath = Path.Combine(_targetFilePath, userDirectory);
        Directory.CreateDirectory(userPath);

        var videoDirectory = Path.Combine(userPath, $"{id}");
        Directory.CreateDirectory(videoDirectory);

        var trustedFileNameForFileStorage = "video";
        if (HttpContext.Request.Method == "POST")
        {
            if (System.IO.File.Exists(Path.Combine(videoDirectory, $"{trustedFileNameForFileStorage}.json")) || System.IO.File.Exists(Path.Combine(videoDirectory, $"{trustedFileNameForFileStorage}.mp4")))
            {
                return Conflict("The file already exists. If you want to update the content, use HTTP PUT");
            }
        }

        var section = await reader.ReadNextSectionAsync();
        string title, description = "";
        bool gotFile = false;

        while (section != null)
        {
            var hasContentDispositionHeader =
                ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition);

            if (!hasContentDispositionHeader)
            {
                return BadRequest();
            }

            if (MultipartRequestHelper.HasFormDataTextContentDisposition(contentDisposition!))
            {
                var formData = section.AsFormDataSection();
                if (formData == null)
                {
                    return BadRequest();
                }

                switch (formData.Name.ToLower())
                {
                    case "title":
                        title = await formData.GetValueAsync();
                        Console.WriteLine(title);
                        break;
                    case "description":
                        description = await formData.GetValueAsync();
                        break;
                }

                section = await reader.ReadNextSectionAsync();
                continue;
            }

            if (gotFile)
            {
                return BadRequest("Only one video upload per request is possible");
            }

            gotFile = true;

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
                    Path.Combine(videoDirectory, $"{trustedFileNameForFileStorage}.mp4")))
                {
                    await targetStream.WriteAsync(streamedFileContent);

                    _logger.LogInformation(
                        "Uploaded file saved to " +
                        "'{VideoPath}' as {TrustedFileNameForFileStorage}.mp4",
                        videoDirectory,
                        trustedFileNameForFileStorage);
                }
            }

            if (MpegHelpers.GenerateDash(_mp4DashPath, _mp4FragmentPath, videoDirectory) != 0)
            {
                return StatusCode(500);
            }

            section = await reader.ReadNextSectionAsync();
        }

        if (!gotFile)
        {
            return BadRequest();
        }
        // TODO: Add title, desc and ID to DB

        return Created(nameof(StreamingController), null);
    }

    [AllowAnonymous]
    [HttpGet("{authorID}/{videoID}/manifest.mpd")]
    public IActionResult GetVideoManifest(string authorID, string videoID)
    {
        if (_targetFilePath == null)
        {
            return StatusCode(500);
        }

        var videoDirectory = Path.Combine(_targetFilePath, authorID, videoID);
        if (!Path.Exists(Path.Combine(videoDirectory, "stream.mpd")))
        {
            return NotFound("DASH manifest doesn't exists");
        }

        return PhysicalFile(Path.Combine(videoDirectory, "stream.mpd"), "application/xml");
    }

    [AllowAnonymous]
    [HttpGet("{authorID}/{videoID}/audio/{p1}/{p2}/{segmentNumber}")]
    public IActionResult GetAudioSegment(string authorID, string videoID, string p1, string p2, string segmentNumber)
    {
        if (_targetFilePath == null)
        {
            return StatusCode(500);
        }

        var videoDirectory = Path.Combine(_targetFilePath, authorID, videoID);
        if (!Path.Exists(Path.Combine(videoDirectory, "audio", p1, p2, segmentNumber)))
        {
            return NotFound("Audio segment doesn't exists");
        }
        return PhysicalFile(Path.Combine(videoDirectory, "audio", p1, p2, segmentNumber), "audio/aac");
    }

    [AllowAnonymous]
    [HttpGet("{authorID}/{videoID}/video/{p1}/{segmentNumber}")]
    public IActionResult GetVideoSegment(string authorID, string videoID, string p1, string segmentNumber)
    {
        if (_targetFilePath == null)
        {
            return StatusCode(500);
        }

        var videoDirectory = Path.Combine(_targetFilePath, authorID, videoID);
        if (!Path.Exists(Path.Combine(videoDirectory, "video", p1, segmentNumber)))
        {
            return NotFound("Video segment doesn't exists");
        }
        return PhysicalFile(Path.Combine(videoDirectory, "video", p1, segmentNumber), "video/mp4");
    }
}