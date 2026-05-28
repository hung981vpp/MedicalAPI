using MedicalRecordService.DTOs;
using MedicalRecordService.Helpers;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api/vital-signs")]
[Authorize]
public sealed class VitalSignsController(VitalSignService service) : ControllerBase
{
    [HttpGet("patient/{patientId:int}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    public IActionResult GetByPatient(int patientId)
    {
        if (!JwtClaimsHelper.IsPatientAllowed(User, patientId))
        {
            return Forbid();
        }

        return Ok(service.GetByPatient(patientId));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Nurse,Receptionist")]
    public IActionResult Create(CreateVitalSignRequest request)
    {
        var created = service.Create(request, JwtClaimsHelper.GetUserId(User));
        return CreatedAtAction(nameof(GetByPatient), new { patientId = created.PatientId }, created);
    }
}
