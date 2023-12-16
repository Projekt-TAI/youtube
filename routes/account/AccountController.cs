using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Protocol;
using TAIBackend.Model;

namespace TAIBackend.routes.account;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme), Route("account")]
[EnableCors("AuthenticatedPolicy")]
public class AccountController : Controller
{
    [AllowAnonymous]
    [HttpGet("facebook-login")]
    public IActionResult FacebookLogin()
    {
        var referer = HttpContext.Request.Headers.Referer;
        var properties = new AuthenticationProperties { RedirectUri = referer };
        return Challenge(properties, FacebookDefaults.AuthenticationScheme);
    }

    [HttpGet("details")]
    public IActionResult Details()
    {
        return Content(new
        {
            id = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
            firstName = User.FindFirst(ClaimTypes.GivenName)?.Value,
            fullName = User.FindFirst(ClaimTypes.Name)?.Value,
            profilePictureSrc = User.FindFirst("urn:facebook:picture")?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value
        }.ToJson(), "application/json");
    }

    [AllowAnonymous]
    [HttpGet("userPic/{userId}")]
    public async Task<IActionResult> GetUserProfilePicture(YoutubeContext db, long userId)
    {
        var user = await db.Accounts.SingleAsync(a => a.Id == userId);
        return Ok(new{profilePicUrl = user.ProfilePicUrl});
    }
}