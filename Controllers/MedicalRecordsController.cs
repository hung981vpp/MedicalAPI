using MedicalRecordService.DTOs;
using MedicalRecordService.Helpers;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api/medical-records")]
[Authorize]
public sealed class MedicalRecordsController(MedicalRecordService.Services.MedicalRecordService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Patient")]
    public IActionResult GetAll([FromQuery] int? patientId)
    {
        if (User.IsInRole("Patient"))
        {
            var claimPatientId = JwtClaimsHelper.GetPatientId(User);
            if (claimPatientId is null || (patientId.HasValue && patientId.Value != claimPatientId.Value))
            {
                return Forbid();
            }

            patientId = claimPatientId.Value;
        }

        return Ok(service.GetAll(patientId));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Doctor,Patient")]
    public IActionResult Get(int id)
    {
        var record = service.Get(id);
        return JwtClaimsHelper.IsPatientAllowed(User, record.PatientId) ? Ok(record) : Forbid();
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor")]
    public IActionResult Update(int id, UpdateMedicalRecordRequest request)
        => Ok(service.Update(id, request, JwtClaimsHelper.GetUserId(User)));

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = "Doctor")]
    public IActionResult Complete(int id, CompleteConsultationRequest request)
        => Ok(service.Complete(id, request, JwtClaimsHelper.GetUserId(User)));

    [HttpPost("{id:int}/referrals")]
    [Authorize(Roles = "Doctor")]
    public IActionResult CreateReferral(int id, CreateReferralRequest request)
        => Ok(service.CreateReferral(id, request, JwtClaimsHelper.GetUserId(User)));

    [HttpGet("{id:int}/referrals")]
    [Authorize(Roles = "Doctor,Patient")]
    public IActionResult GetReferrals(int id)
    {
        var record = service.Get(id);
        return JwtClaimsHelper.IsPatientAllowed(User, record.PatientId) ? Ok(service.GetReferrals(id)) : Forbid();
    }
}
