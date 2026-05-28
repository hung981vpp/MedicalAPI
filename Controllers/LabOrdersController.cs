using MedicalRecordService.DTOs;
using MedicalRecordService.Helpers;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class LabOrdersController(LabOrderService service, MedicalRecordService.Services.MedicalRecordService records) : ControllerBase
{
    [HttpPost("medical-records/{recordId:int}/lab-orders")]
    [Authorize(Roles = "Doctor")]
    public IActionResult Create(int recordId, CreateLabOrderRequest request)
        => Ok(service.Create(recordId, request, JwtClaimsHelper.GetUserId(User)));

    [HttpGet("medical-records/{recordId:int}/lab-orders")]
    [Authorize(Roles = "Doctor,Patient")]
    public IActionResult GetByRecord(int recordId)
    {
        var record = records.Get(recordId);
        return JwtClaimsHelper.IsPatientAllowed(User, record.PatientId) ? Ok(service.GetByRecord(recordId)) : Forbid();
    }

    [HttpPut("lab-orders/{id:int}/results")]
    [Authorize(Roles = "Doctor")]
    public IActionResult UpdateResults(int id, UpdateLabOrderResultsRequest request)
        => Ok(service.UpdateResults(id, request));
}
