using MedicalRecordService.DTOs;
using MedicalRecordService.Services;

namespace MedicalRecordService.Consumers;

public sealed class AppointmentCheckedInConsumer(SyncService syncService)
{
    public void Consume(AppointmentCheckedInEvent message)
    {
        syncService.UpsertAppointment(message);
    }
}
