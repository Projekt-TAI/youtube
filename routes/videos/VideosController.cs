using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using System.Security.Claims;
using TAIBackend.Model;
using TAIBackend.routes.videos.models;
using TAIBackend.services;

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
                thumbnailSrc = $"/videos/{video.Id}/thumbnail.jpg"
            }),
            count = db.Videos.Count()
        });
    }

    [HttpGet("{videoID}/manifest.mpd")]
    public async Task<IActionResult> GetVideoManifestFromID(YoutubeContext db, int videoID)
    {
        Video video = await db.Videos.SingleAsync(v => v.Id==videoID);

        return await GetVideoManifest(db, video.Owneraccountid.ToString(), video.Id);
    }

    [HttpGet("{videoID}/audio/{p1}/{p2}/{segmentNumber}")]
    public async Task<IActionResult> GetAudioSegmentFromID(YoutubeContext db, int videoID, string p1, string p2,
        string segmentNumber)
    {
        Video video = await db.Videos.SingleAsync(v => v.Id==videoID);

        return GetAudioSegment(video.Owneraccountid.ToString(), video.Id.ToString(), p1, p2, segmentNumber);
    }

    [HttpGet("{videoID}/video/{p1}/{segmentNumber}")]
    public async Task<IActionResult> GetVideoSegmentFromID(YoutubeContext db, int videoID, string p1,
        string segmentNumber)
    {
        Video video = await db.Videos.SingleAsync(v => v.Id==videoID);

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

        var videoDirectory = Path.Join(_targetFilePath, authorID, videoID.ToString());
        if (!Path.Exists(Path.Join(videoDirectory, "stream.mpd")))
        {
            return NotFound("DASH manifest doesn't exists");
        }

        return PhysicalFile(Path.Join(videoDirectory, "stream.mpd"), "application/xml");
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
            return NotFound($"Audio segment {Path.Combine(videoDirectory, "audio", p1, p2, segmentNumber)} doesn't exists");
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
            return NotFound($"Video {Path.Combine(videoDirectory, "video", p1, segmentNumber)} segment doesn't exists");
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
                thumbnailSrc = $"/videos/{video.Id}/thumbnail.jpg"
            })
            .ToJson() ?? "null");
    }

    [HttpGet("{videoId}/comments")]
    public async Task<IActionResult> GetVideoComments(YoutubeContext db, long videoId,
        [FromQuery(Name = "pageNumber")] int pageNumber, [FromQuery(Name = "pageSize")] int pageSize)
    {
        var comments = await db.Comments
            .Where(c => c.Videoid == videoId)
            .Include(c => c.Commenter)
            .OrderByDescending(c => c.Id)
            .Skip(pageNumber * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = comments.ToArray().Select(comment => new
            {
                id = comment.Id,
                userId = comment.Commenterid,
                videoId = comment.Videoid,
                data = comment.Data,
                createdAt = comment.CreatedAt,
                fullName = comment.Commenter.Fullname
            }),
            count = db.Comments.Count(c => c.Videoid == videoId)
        });
    }

    [HttpPost("{videoId}/comments")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> AddVideoComment(YoutubeContext db, long videoId, [FromBody] AddVideoCommentModel body)
    {
        var commenterId = User.FindFirst(ClaimTypes.NameIdentifier);

        if (commenterId == null)
        {
            return StatusCode(500);
        }

        Comment newComment = new Comment
        {
            Commenterid = long.Parse(commenterId.Value!),
            Videoid = (int)videoId,
            Data = body.data
        };

        db.Comments.Add(newComment);

        await db.SaveChangesAsync();

        return Ok(new
        {
            id = newComment.Id,
            data = newComment.Data,
            createdAt = newComment.CreatedAt,
            userId = newComment.Commenterid,
            videoId = newComment.Videoid
        });
    }
    
    [HttpGet("{videoId}/details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVideoDetails(YoutubeContext db, int videoId)
    {
        try
        {
            var v =await db.Videos.Include(v => v.Likes).Include(v => v.Owneraccount).SingleAsync(v => v.Id == videoId);
            var likes = v.Likes.Count;
            
            return Ok(new
            {
                id = v.Id,
                createdAt = v.CreatedAt,
                userId = v.Owneraccountid,
                
                views = v.Views,
                likes = likes
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPost("{videoId}/share/{userId}")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> ShareVideo(YoutubeContext db, MailBuilder mailBuilder, MailSender mailSender, 
        long videoId, long userId)
    {
        var sharingUserId = User.FindFirst(ClaimTypes.NameIdentifier);

        if (sharingUserId == null)
        {
            return StatusCode(500);
        }

        var referer = HttpContext.Request.Headers.Referer;
        var videoUrl = $"{referer}watch/{videoId}";

        var user = await db.Accounts
            .Where(a => a.Id == userId)
            .SingleAsync();

        var sharingUser = await db.Accounts
            .Where(a => a.Id == long.Parse(sharingUserId.Value))
            .SingleAsync();

        if (user == null || sharingUser == null)
        {
            return NotFound();
        }

        var emailSubject = "A user shared a video with you";
        var emailBody = $"User {sharingUser.Fullname} shared a <a href=\"{videoUrl}\">Video</a>";
        var recipients = new List<string> { user.Email };

        await mailSender.SendMail(mailBuilder.BuildMail(recipients, emailSubject, emailBody));

        return Ok();
    }
}
