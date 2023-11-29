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
    private class VideoDescriptor
    {
        [JsonProperty("id")]
        public int? Id;
        [JsonProperty("title")]
        public string? Title;
    }


    private readonly long _fileSizeLimit;
    private readonly ILogger<StreamingController> _logger;
    private readonly string[] _permittedExtensions = { ".mp4" };
    private readonly string? _targetFilePath;

    private readonly string _mp4DashPath;
    private readonly string _mp4FragmentPath;

    private static readonly FormOptions _defaultFormOptions = new FormOptions();

    public StreamingController(ILogger<StreamingController> logger,
         IConfiguration config)
    {
        _logger = logger;
        _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
        _targetFilePath = config.GetValue<string>("StoredFilesPath");
        _mp4DashPath = config.GetValue<string>("MP4DashPath");
        _mp4FragmentPath = config.GetValue<string>("MP4FragmentPath");
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
        var userDirectory = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(User.FindFirst(ClaimTypes.NameIdentifier)!.Value));
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


        var videoDirectory = Path.Combine(userPath, $"{videoDescriptor.Id}");
        Directory.CreateDirectory(videoDirectory);

        var trustedFileNameForFileStorage = "video";
        if (HttpContext.Request.Method == "POST")
        {
            if (System.IO.File.Exists(Path.Combine(videoDirectory, $"{trustedFileNameForFileStorage}.json")) || System.IO.File.Exists(Path.Combine(videoDirectory, $"{trustedFileNameForFileStorage}.mp4")))
            {
                return Conflict("The file already exists. If you want to update the content, use HTTP PUT");
            }
        }

        using (var targetStream = System.IO.File.Create(
            Path.Combine(videoDirectory, $"{trustedFileNameForFileStorage}.json")))
        {
            await targetStream.WriteAsync(Encoding.ASCII.GetBytes(json));

            _logger.LogInformation(
                "Uploaded file config, saved to " +
                "'{VideoPath}' as {TrustedFileNameForFileStorage}.json",
                videoDirectory,
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

        return Created(nameof(StreamingController), null);
    }

    [AllowAnonymous]
    [HttpGet("{authorID}/{videoID}/manifest.mpd")]
    public IActionResult GetVideoManifest(string authorID, string videoID)
    {
        var userDirectory = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(authorID));
        if (_targetFilePath == null || userDirectory == null)
        {
            return StatusCode(500);
        }

        var videoDirectory = Path.Combine(_targetFilePath, userDirectory, videoID);

        return PhysicalFile(Path.Combine(videoDirectory, "stream.mpd"), "application/xml");
    }

    [AllowAnonymous]
    [HttpGet("{authorID}/{videoID}/audio/{p1}/{p2}/{segmentNumber}")]
    public IActionResult GetAudioSegment(string authorID, string videoID, string p1, string p2, string segmentNumber)
    {
        var userDirectory = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(authorID));
        if (_targetFilePath == null || userDirectory == null)
        {
            return StatusCode(500);
        }

        var videoDirectory = Path.Combine(_targetFilePath, userDirectory, videoID);
        return PhysicalFile(Path.Combine(videoDirectory, "audio", p1, p2, segmentNumber), "audio/aac");
    }

    [AllowAnonymous]
    [HttpGet("{authorID}/{videoID}/video/{p1}/{segmentNumber}")]
    public IActionResult GetVideoSegment(string authorID, string videoID, string p1, string segmentNumber)
    {
        var userDirectory = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(authorID));
        if (_targetFilePath == null || userDirectory == null)
        {
            return StatusCode(500);
        }

        var videoDirectory = Path.Combine(_targetFilePath, userDirectory, videoID);
        return PhysicalFile(Path.Combine(videoDirectory,"video", p1, segmentNumber), "video/mp4");
    }
}