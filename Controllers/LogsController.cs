using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize(Roles = "Admin")]
public sealed class LogsController(MedicalRecordService.Services.MedicalRecordService service) : ControllerBase
{
    [HttpGet("medical-records")]
    public IActionResult Get([FromQuery] int? recordId) => Ok(service.GetLogs(recordId));
}
