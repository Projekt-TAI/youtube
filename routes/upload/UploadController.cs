using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;

namespace TAIBackend.routes.upload;

[Authorize(AuthenticationSchemes = FacebookDefaults.AuthenticationScheme)]
public class UploadController : Controller
{
    [HttpGet("upload")]
    public IActionResult Upload()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(userId);
    }
}