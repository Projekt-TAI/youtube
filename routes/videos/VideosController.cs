using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using TAIBackend.Model;

namespace TAIBackend.routes.videos;

[Route("videos")]
public class VideosController : Controller
{
    private readonly string _targetFilePath;

    public VideosController(IConfiguration config)
    {
        _targetFilePath = config.GetValue<string>("StoredFilesPath") ?? throw new InvalidOperationException();
    }

    [HttpGet(""), HttpGet("/")]
    public async Task<IActionResult> GetVideos(YoutubeContext db, [FromQuery(Name = "pageNumber")] int pageNumber,
        [FromQuery(Name = "pageSize")] int pageSize)
    {
        var vids = await db.Videos.OrderByDescending(v => v.Id).Skip(pageNumber * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = vids.ToArray().Select(video => new
            {
                id = video.Id, authorID = video.Owneraccountid, title = video.Title, description = video.Description,
                category = video.Category, createdAt = video.CreatedAt.ToUniversalTime(), views = video.Views,
                thumbnailSrc = $"videos/{video.Id}/thumbnail.jpg"
            }),
            count = db.Videos.Count()
        });
    }

    [HttpGet("{videoID}/manifest.mpd")]
    public async Task<IActionResult> GetVideoManifestFromID(YoutubeContext db, int id)
    {
        Video? video = await db.Videos.FindAsync(id);
        if (video == null)
        {
            return NotFound("Video with specified id does not exist");
        }

        return await GetVideoManifest(db, video.Owneraccountid.ToString(), video.Id);
    }

    [HttpGet("{videoID}/audio/{p1}/{p2}/{segmentNumber}")]
    public async Task<IActionResult> GetAudioSegmentFromID(YoutubeContext db, int videoID, string p1, string p2,
        string segmentNumber)
    {
        Video? video = await db.Videos.FindAsync(videoID);
        if (video == null)
        {
            return NotFound("Video with specified id does not exist");
        }

        return GetAudioSegment(video.Owneraccountid.ToString(), video.Id.ToString(), p1, p2, segmentNumber);
    }

    [HttpGet("{videoID}/video/{p1}/{segmentNumber}")]
    public async Task<IActionResult> GetVideoSegmentFromID(YoutubeContext db, int videoID, string p1,
        string segmentNumber)
    {
        Video? video = await db.Videos.FindAsync(videoID);
        if (video == null)
        {
            return NotFound("Video with specified id does not exist");
        }

        return GetVideoSegment(video.Owneraccountid.ToString(), video.Id.ToString(), p1, segmentNumber);
    }

    [HttpGet("{authorID}/{videoID}/manifest.mpd")]
    public async Task<IActionResult> GetVideoManifest(YoutubeContext db, string authorID, int videoID)
    {
        if (_targetFilePath == null)
        {
            return StatusCode(500);
        }

        var vid = await db.Videos.SingleAsync(v => v.Id == videoID);
        vid.Views += 1;
        await db.SaveChangesAsync();

        var videoDirectory = Path.Combine(_targetFilePath, authorID, videoID.ToString());
        if (!Path.Exists(Path.Combine(videoDirectory, "stream.mpd")))
        {
            return NotFound("DASH manifest doesn't exists");
        }

        return PhysicalFile(Path.Combine(videoDirectory, "stream.mpd"), "application/xml");
    }

    [HttpGet("{videoID}/thumbnail.jpg")]
    public async Task<IActionResult> GetVideoThumbnail(YoutubeContext db, int videoID)
    {
        if (_targetFilePath == null)
        {
            return StatusCode(500);
        }

        var vid = await db.Videos.SingleAsync(v => v.Id == videoID);

        var videoDirectory = Path.Combine(_targetFilePath, vid.Owneraccountid.ToString(), videoID.ToString());
        if (!Path.Exists(Path.Combine(videoDirectory, "thumbnail.jpg")))
        {
            return NotFound("Thumbnail doesn't exists");
        }

        return PhysicalFile(Path.Combine(videoDirectory, "thumbnail.jpg"), "image/jpg");
    }

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

    [HttpGet("{authorID}")]
    public async Task<IActionResult> GetVideosFromUser(YoutubeContext db, long authorId,
        [FromQuery(Name = "pageNumber")] int pageNumber, [FromQuery(Name = "pageSize")] int pageSize)
    {
        var accs = await db.Videos.Where(v => v.Owneraccountid == authorId).OrderByDescending(v => v.Id)
            .Skip(pageNumber * pageSize).Take(pageSize).ToListAsync();
        return Ok(accs?.ToArray()
            .Select(video => new
            {
                video.Id, video.Title, video.Description, video.Category, views = video.Views,
                thumbnailSrc = $"videos/{video.Id}/thumbnail.jpg"
            })
            .ToJson() ?? "null");
    }
}