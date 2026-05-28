using MedicalRecordService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordService.Controllers;

[ApiController]
[Route("api/outbox")]
[Authorize(Roles = "Admin")]
public sealed class OutboxController(MedicalDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        lock (db.Gate)
        {
            return Ok(db.OutboxMessages.OrderByDescending(x => x.CreatedAt).ToList());
        }
    }
}
