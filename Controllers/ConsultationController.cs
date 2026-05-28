using MedicalRecordService.DTOs;
using MedicalRecordService.Helpers;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api/consultations")]
[Authorize(Roles = "Doctor")]
public sealed class ConsultationController(ConsultationService service) : ControllerBase
{
    [HttpPost("start")]
    public IActionResult Start(StartConsultationRequest request)
    {
        var userId = JwtClaimsHelper.GetUserId(User);
        if (request.DoctorId != userId)
        {
            return Forbid();
        }

        return Ok(service.Start(request));
    }

    [HttpPost("{sessionId:int}/complete")]
    public IActionResult Complete(int sessionId)
        => Ok(service.End(sessionId, JwtClaimsHelper.GetUserId(User), cancel: false));

    [HttpPost("{sessionId:int}/cancel")]
    public IActionResult Cancel(int sessionId)
        => Ok(service.End(sessionId, JwtClaimsHelper.GetUserId(User), cancel: true));

    [HttpPost("medical-records/{recordId:int}/locks")]
    public IActionResult AcquireLock(int recordId)
        => Ok(service.AcquireLock(recordId, JwtClaimsHelper.GetUserId(User)));

    [HttpPost("medical-records/{recordId:int}/locks/refresh")]
    public IActionResult RefreshLock(int recordId)
        => Ok(service.RefreshLock(recordId, JwtClaimsHelper.GetUserId(User)));

    [HttpDelete("medical-records/{recordId:int}/locks")]
    public IActionResult ReleaseLock(int recordId)
    {
        service.ReleaseLock(recordId, JwtClaimsHelper.GetUserId(User));
        return NoContent();
    }
}
