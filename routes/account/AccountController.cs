using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TAIBackend.routes.account;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme), Route("account")]
public class AccountController : Controller
{
    [AllowAnonymous]
    [HttpGet("facebook-login")]
    public IActionResult FacebookLogin()
    {
        var properties = new AuthenticationProperties { RedirectUri = "https://localhost:3000" };
        return Challenge(properties, FacebookDefaults.AuthenticationScheme);
    }

    [HttpGet("details")]
    public IActionResult Details()
    {
        return Json("a");
    }
    
}