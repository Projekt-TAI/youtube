using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TAIBackend.routes.upload;

[Authorize]
public class UploadController : Controller
{
    [HttpGet("upload")]
    public IActionResult Upload()
    {
        return Ok();
    }
}