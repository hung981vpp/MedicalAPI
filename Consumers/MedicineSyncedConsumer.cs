using MedicalRecordService.DTOs;
using MedicalRecordService.Services;

namespace MedicalRecordService.Consumers;

public sealed class MedicineSyncedConsumer(SyncService syncService)
{
    public void Consume(MedicineSyncedEvent message)
    {
        syncService.UpsertMedicine(message);
    }
}
