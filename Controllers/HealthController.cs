using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            service = "MedicalRecordService",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
