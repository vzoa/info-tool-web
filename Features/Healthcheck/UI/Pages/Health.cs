using Microsoft.AspNetCore.Mvc;

namespace ZoaReference.Features.Healthcheck.UI.Pages;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult HealthCheck()
    {
        return Ok();
    }
}