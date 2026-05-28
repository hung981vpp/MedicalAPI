using MedicalRecordService.DTOs;
using MedicalRecordService.Helpers;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class PrescriptionsController(PrescriptionService service) : ControllerBase
{
    [HttpPost("medical-records/{recordId:int}/prescriptions")]
    [Authorize(Roles = "Doctor")]
    public IActionResult Create(int recordId, CreatePrescriptionRequest request)
        => Ok(service.Create(recordId, request, JwtClaimsHelper.GetUserId(User)));

    [HttpGet("prescriptions/{id:int}")]
    [Authorize(Roles = "Doctor,Patient")]
    public IActionResult Get(int id)
    {
        var prescription = service.Get(id);
        return JwtClaimsHelper.IsPatientAllowed(User, prescription.PatientId) ? Ok(prescription) : Forbid();
    }

    [HttpPut("prescriptions/{id:int}")]
    [Authorize(Roles = "Doctor")]
    public IActionResult Update(int id, CreatePrescriptionRequest request)
        => Ok(service.Update(id, request, JwtClaimsHelper.GetUserId(User)));

    [HttpPost("prescriptions/{id:int}/finalize")]
    [Authorize(Roles = "Doctor")]
    public IActionResult Finalize(int id)
        => Ok(service.Finalize(id, JwtClaimsHelper.GetUserId(User)));
}
