using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using TAIBackend.Utilities;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TAIBackend.Model;
using NuGet.Protocol;

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
        _targetFilePath = config.GetValue<string>("StoredFilesPath") ?? throw new InvalidOperationException();
        _mp4DashPath = config.GetValue<string>("MP4DashPath") ?? throw new InvalidOperationException();
        _mp4FragmentPath = config.GetValue<string>("MP4FragmentPath") ?? throw new InvalidOperationException();
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPut("upload/{id}")]
    [RequiresUserAccount]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> EditPhysical(YoutubeContext db, int id)
    {
        if (Request.ContentType == null)
        {
            return BadRequest();
        }

        Video? video = await db.Videos.FindAsync(id);
        if (video == null)
        {
            return NotFound("Video with specified ID does not exist");
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
        var section = await reader.ReadNextSectionAsync();
        var title = "";
        var description = "";
        var category = "";
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
                        video.Title = title;
                        break;
                    case "description":
                        description = await formData.GetValueAsync();
                        video.Description = description;
                        break;
                    case "category":
                        category = await formData.GetValueAsync();
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

        if (!gotFile || String.IsNullOrWhiteSpace(title))
        {
            return BadRequest();
        }

        if (String.IsNullOrWhiteSpace(category))
        {
            video.Category = 0;
        }
        else
        {
            video.Category = int.Parse(category);
        }

        await db.SaveChangesAsync();
        
        return Content(new { id = video.Id, title = video.Title, description = video.Description }.ToJson(), "application/json");
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("upload")]
    [RequiresUserAccount]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> CreatePhysical(YoutubeContext db)
    {
        if (Request.ContentType == null)
        {
            return BadRequest();
        }

        var id = 0;
        if (db.Videos.Any())
        {
            var maxId = await db.Videos.MaxAsync(table => table.Id);
            id = maxId + 1;
        }

        Video video = new Video
        {
            Id = id
        };

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

        var section = await reader.ReadNextSectionAsync();
        var title = "";
        var description = "";
        string? category = "";
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
                        video.Title = title;
                        break;
                    case "description":
                        description = await formData.GetValueAsync();
                        video.Description = description;
                        break;
                    case "category":
                        category = await formData.GetValueAsync();
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

        if (!gotFile || String.IsNullOrWhiteSpace(title))
        {
            return BadRequest();
        }

        if (String.IsNullOrWhiteSpace(category))
        {
            video.Category = 0;
        }
        else
        {
            video.Category = int.Parse(category);
        }

        video.ThumbnailSrc = Path.Combine(videoDirectory, "thumbnail.jpg");

        var owner = await db.Accounts.FindAsync(Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value));
        if (owner == null)
        {
            return StatusCode(500, "Owner does not exist in DB");
        }

        video.Owneraccountid = Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        db.Add(video);
        await db.SaveChangesAsync();

        return StatusCode(201, new { id = video.Id, title = video.Title, description = video.Description, category = video.Category }.ToJson());
    }
}