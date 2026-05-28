using MedicalRecordService.DTOs;
using MedicalRecordService.Helpers;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api/patient-profiles")]
[Authorize]
public sealed class PatientProfilesController(PatientProfileService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public IActionResult GetAll() => Ok(service.GetAll());

    [HttpGet("{patientId:int}")]
    [Authorize(Roles = "Admin,Doctor,Patient")]
    public IActionResult GetByPatientId(int patientId)
    {
        if (!JwtClaimsHelper.IsPatientAllowed(User, patientId))
        {
            return Forbid();
        }

        var profile = service.GetByPatientId(patientId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("{patientId:int}")]
    [Authorize(Roles = "Doctor")]
    public IActionResult Upsert(int patientId, UpsertPatientProfileRequest request)
    {
        if (patientId != request.PatientId)
        {
            return BadRequest("Route patientId must match body PatientId.");
        }

        return Ok(service.Upsert(request));
    }
}
