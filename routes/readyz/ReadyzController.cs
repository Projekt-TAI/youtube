using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TAIBackend.routes.readyz;

public class ReadyzController : Controller
{
    [HttpGet("readyz")]
    public IActionResult Readyz()
    {
        return Ok();
    }
}