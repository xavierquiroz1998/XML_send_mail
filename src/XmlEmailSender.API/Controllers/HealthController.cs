using Microsoft.AspNetCore.Mvc;

namespace XmlEmailSender.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "XmlEmailSender",
        timestamp = DateTime.UtcNow
    });
}
