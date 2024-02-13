using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NuGet.Protocol;
using System.Security.Claims;
using TAIBackend.Model;
using TAIBackend.routes.videos.models;
using TAIBackend.services;
using TAIBackend.Utilities;

namespace TAIBackend.routes.videos;

[Route("videos")]
public class VideosController : Controller
{
    private readonly string _targetFilePath;

    public VideosController(IConfiguration config)
    {
        _targetFilePath = config.GetValue<string>("StoredFilesPath") ?? throw new InvalidOperationException();
    }
    
    [HttpDelete("{videoId}")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> DeleteVideo(YoutubeContext db, long videoId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return StatusCode(500);
        }

        var userIdParsed = long.Parse(userId.Value);
        var video = await db.Videos.SingleAsync(v => v.Id == videoId);
        if (video.OwneraccountId != userIdParsed)
        {
            return Forbid();
        }

        db.Videos.Remove(video);
        
        var videoDirectory = Path.Combine(_targetFilePath, video.OwneraccountId.ToString(), videoId.ToString());
        if (Path.Exists(videoDirectory))
        {
            Directory.Delete(videoDirectory,true);
        }

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpGet(""), HttpGet("/")]
    public IActionResult GetVideos(YoutubeContext db, [FromQuery(Name = "pageNumber")] int pageNumber,
        [FromQuery(Name = "pageSize")] int pageSize, [FromQuery(Name = "searchText")] string? searchText,
        [FromQuery(Name = "categoryId")] int? categoryId)
    {
        Func<Video, bool> predicate = delegate (Video v)
        {
            return (searchText != null ? v.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 : true) && (categoryId != null ? v.Category == categoryId : true);
        };

        var vids = db.Videos
            .Include(v => v.Owneraccount)
            .Where(predicate)
            .OrderByDescending(v => v.Id)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            data = vids.ToArray().Select(video => VideoMapper.Map(video)),
            count = db.Videos.Where(predicate).Count()
        });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserVideos(YoutubeContext db, [FromQuery(Name = "pageNumber")] int pageNumber,
        [FromQuery(Name = "pageSize")] int pageSize, long userId)
    {
        var query = db.Videos
            .Include(v => v.Owneraccount)
            .Where(v => v.OwneraccountId == userId)
            .OrderByDescending(v => v.Id);

        var subscriptionVideos = await query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var count = query.Count();

        return Ok(new
        {
            data = subscriptionVideos.ToArray().Select(video => VideoMapper.Map(video)),
            count
        });
    }

    [HttpGet("subscriptions")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> GetSubscriptionVideos(YoutubeContext db, [FromQuery(Name = "pageNumber")] int pageNumber,
        [FromQuery(Name = "pageSize")] int pageSize)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return StatusCode(500);
        }

        var userIdParsed = long.Parse(userId.Value);

        var query = db.Subscriptions
            .Include(s => s.Subscribedaccount)
            .ThenInclude(sa => sa.Videos)
            .ThenInclude(v => v.Owneraccount)
            .Where(s => s.OwneraccountId == userIdParsed)
            .SelectMany(s => s.Subscribedaccount.Videos)
            .OrderByDescending(v => v.Id);

        var subscriptionVideos = await query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var count = query.Count();

        return Ok(new
        {
            data = subscriptionVideos.ToArray().Select(video => VideoMapper.Map(video)),
            count
        });
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingVideos(YoutubeContext db, [FromQuery(Name = "pageNumber")] int pageNumber,
        [FromQuery(Name = "pageSize")] int pageSize)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var query = db.Videos
            .Include(v => v.Owneraccount)
            .Where(v => v.CreatedAt >= sevenDaysAgo)
            .OrderByDescending(v => v.Views);

        var trendingVideos = await query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var count = query.Count();

        return Ok(new
        {
            data = trendingVideos.ToArray().Select(video => VideoMapper.Map(video)),
            count
        });
    }

    [HttpGet("{videoID}/manifest.mpd")]
    public async Task<IActionResult> GetVideoManifestFromID(YoutubeContext db, int videoID)
    {
        Video video = await db.Videos.SingleAsync(v => v.Id == videoID);

        return await GetVideoManifest(db, video.OwneraccountId.ToString(), video.Id);
    }

    [HttpGet("{videoID}/audio/{p1}/{p2}/{segmentNumber}")]
    public async Task<IActionResult> GetAudioSegmentFromID(YoutubeContext db, int videoID, string p1, string p2,
        string segmentNumber)
    {
        Video video = await db.Videos.SingleAsync(v => v.Id == videoID);

        return GetAudioSegment(video.OwneraccountId.ToString(), video.Id.ToString(), p1, p2, segmentNumber);
    }

    [HttpGet("{videoID}/video/{p1}/{segmentNumber}")]
    public async Task<IActionResult> GetVideoSegmentFromID(YoutubeContext db, int videoID, string p1,
        string segmentNumber)
    {
        Video video = await db.Videos.SingleAsync(v => v.Id == videoID);

        return GetVideoSegment(video.OwneraccountId.ToString(), video.Id.ToString(), p1, segmentNumber);
    }

    [HttpGet("{authorID}/{videoID}/manifest.mpd")]
    public async Task<IActionResult> GetVideoManifest(YoutubeContext db, string authorID, long videoID)
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

        var videoDirectory = Path.Combine(_targetFilePath, vid.OwneraccountId.ToString(), videoID.ToString());
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
        var accs = await db.Videos.Where(v => v.OwneraccountId == authorId).OrderByDescending(v => v.Id)
            .Skip(pageNumber * pageSize).Take(pageSize).ToListAsync();
        return Ok(accs?.ToArray()
            .Select(video => new
            {
                video.Id,
                video.Title,
                video.Description,
                video.Category,
                views = video.Views,
                thumbnailSrc = $"/videos/{video.Id}/thumbnail.jpg"
            })
            .ToJson() ?? "null");
    }

    [HttpPatch("{videoId}")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> EditVideo(YoutubeContext db, long videoId, [FromBody] EditVideoModel body)
    {
        var video = await db.Videos.Where(v => v.Id == videoId).FirstOrDefaultAsync();

        if (video == null)
        {
            return NotFound();
        }

        var commenterId = User.FindFirst(ClaimTypes.NameIdentifier);

        if (commenterId == null)
        {
            return StatusCode(500);
        }

        var commenterIdParsed = long.Parse(commenterId.Value);

        if (video.OwneraccountId != commenterIdParsed)
        {
            return Forbid();
        }

        video.Title = body.title;
        video.Description = body.description;
        video.Category = body.category;

        await db.SaveChangesAsync();

        return Ok(VideoMapper.Map(video));
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
                fullName = comment.Commenter.Fullname,
                profilePictureSrc = comment.Commenter.ProfilePicUrl
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

        return StatusCode(201, new
        {
            id = newComment.Id,
            data = newComment.Data,
            isEdited = newComment.IsEdited,
            createdAt = newComment.CreatedAt,
            userId = newComment.Commenterid,
            profilePictureSrc = newComment.Commenter.ProfilePicUrl,
            videoId = newComment.Videoid
        });
    }

    [HttpPatch("{videoId}/comments/{commentId}")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> AddVideoComment(YoutubeContext db, long videoId, long commentId,
        [FromBody] AddVideoCommentModel body)
    {
        var commenterId = User.FindFirst(ClaimTypes.NameIdentifier);

        if (commenterId == null)
        {
            return StatusCode(500);
        }

        var commenterIdParsed = long.Parse(commenterId.Value);

        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.Videoid == videoId && c.Commenterid == commenterIdParsed);

        if (comment == null)
        {
            return NotFound();
        }

        comment.Data = body.data;
        comment.CreatedAt = DateTime.UtcNow;
        comment.IsEdited = true;

        await db.SaveChangesAsync();

        return Ok(new
        {
            id = comment.Id,
            data = comment.Data,
            isEdited = comment.IsEdited,
            createdAt = comment.CreatedAt,
            userId = comment.Commenterid,
            profilePictureSrc = comment.Commenter.ProfilePicUrl,
            videoId = comment.Videoid,
        });
    }

    [HttpDelete("{videoId}/comments/{commentId}")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> DeleteComment(YoutubeContext db, int videoId, int commentId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return StatusCode(500);
        }

        var userIdParsed = long.Parse(userId);

        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.Videoid == videoId && c.Commenterid == userIdParsed);

        if (comment == null)
        {
            return NotFound();
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("{videoId}/details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVideoDetails(YoutubeContext db, int videoId)
    {
        try
        {
            var watcherId = User.FindFirst(ClaimTypes.NameIdentifier);

            var v = await db.Videos
                .Include(v => v.Likes)
                .Include(v => v.Owneraccount)
                .SingleAsync(v => v.Id == videoId);

            var likes = v.Likes.ToList();
            var likeCount = likes.FindAll(l => l.Unlike == false).Count();
            var dislikeCount = likes.FindAll(l => l.Unlike == true).Count();

            Like? like = null;
            if (watcherId != null)
            {
                var watcherIdParsed = long.Parse(watcherId.Value);

                like = await db.Likes
                    .Where(l => l.VideoId == v.Id && l.AccountId == watcherIdParsed)
                    .FirstOrDefaultAsync();
            }
            var isLiked = false;
            var isDisliked = false;

            if (like != null)
            {
                isLiked = !like.Unlike;
                isDisliked = like.Unlike;
            }
            return Ok(new
            {
                id = v.Id,
                createdAt = v.CreatedAt,
                userId = v.OwneraccountId,
                userFullName = v.Owneraccount.Fullname,
                profilePictureSrc = v.Owneraccount.ProfilePicUrl,

                views = v.Views,
                likes = likeCount,
                dislikes = dislikeCount,
                subscriptions = v.Owneraccount.Subscribers.Count(),

                title = v.Title,
                description = v.Description,
                category = v.Category,

                isLiked = isLiked,
                isDisliked = isDisliked
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpGet("{videoId}/info")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVideoInfo(YoutubeContext db, int videoId)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId);

        if (video == null)
        {
            return NotFound();
        }

        return Ok(VideoMapper.Map(video));
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

        var sharingUserIdParsed = long.Parse(sharingUserId.Value);

        var referer = HttpContext.Request.Headers.Referer;
        var videoUrl = $"{referer}watch/{videoId}";

        var user = await db.Accounts
            .Where(a => a.Id == userId)
            .SingleAsync();

        var sharingUser = await db.Accounts
            .Where(a => a.Id == sharingUserIdParsed)
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


    [HttpPost("{videoId}/like")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> LikeVideo(YoutubeContext db, [FromBody] AddVideoLikeModel body, long videoId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return StatusCode(500);
        }

        var userIdParsed = long.Parse(userId.Value!);

        bool likeState = body.value == -1;
        var like = await db.Likes
            .FirstOrDefaultAsync(l => l.VideoId == videoId && l.AccountId == userIdParsed);

        int statusCode;

        if (like == null)
        {
            like = new Like
            {
                VideoId = videoId,
                AccountId = userIdParsed,
                Unlike = likeState
            };

            db.Likes.Add(like);
            await db.SaveChangesAsync();

            statusCode = 201; // Created status code
        }
        else
        {
            like.Unlike = likeState;

            await db.SaveChangesAsync();

            statusCode = 200; // Updated status code
        }

        return StatusCode(statusCode, new
        {
            isLiked = !like.Unlike,
            isDisliked = like.Unlike
        });
    }

    [HttpDelete("{videoId}/like")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> DeleteLike(YoutubeContext db, int videoId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return StatusCode(500);
        }

        var userIdParsed = long.Parse(userId);

        var like = await db.Likes
            .FirstOrDefaultAsync(l => l.VideoId == videoId && l.AccountId == userIdParsed);

        db.Likes.Remove(like);
        await db.SaveChangesAsync();

        return Ok();
    }
}
