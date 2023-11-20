using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;

namespace TAIBackend.routes.oauth;

[Route("oauth2")]
public class OauthController : Controller
{
    [HttpGet("test",Name = "test")]
    public IActionResult Test()
    {
        return Content("TEST");
    }
}