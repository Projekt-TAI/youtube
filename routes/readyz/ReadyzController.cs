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