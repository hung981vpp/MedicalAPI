using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class LocalCatalogController(SyncService service) : ControllerBase
{
    [HttpGet("exam-queue")]
    [Authorize(Roles = "Doctor,Nurse,Receptionist")]
    public IActionResult GetQueue([FromQuery] int? doctorId) => Ok(service.GetQueue(doctorId));

    [HttpGet("local-medicines")]
    [Authorize(Roles = "Doctor")]
    public IActionResult GetMedicines([FromQuery] string? q) => Ok(service.GetMedicines(q));
}
