using MedicalRecordService.DTOs;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api/events")]
[Authorize(Roles = "Admin")]
public sealed class EventsController(SyncService service) : ControllerBase
{
    [HttpPost("appointment-checked-in")]
    public IActionResult AppointmentCheckedIn(AppointmentCheckedInEvent evt) => Ok(service.UpsertAppointment(evt));

    [HttpPost("medicine-synced")]
    public IActionResult MedicineSynced(MedicineSyncedEvent evt) => Ok(service.UpsertMedicine(evt));
}
