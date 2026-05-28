namespace MedicalRecordService.Models;

public enum ConsultationStatus
{
    Active,
    Completed,
    Cancelled
}

public enum MedicalRecordStatus
{
    Draft,
    Locked,
    Completed,
    Cancelled
}

public enum LabOrderStatus
{
    Ordered,
    Resulted,
    Cancelled
}

public enum PrescriptionStatus
{
    Draft,
    Finalized,
    Cancelled
}

public enum PrescriptionType
{
    Insurance,
    Service
}

public enum QueueStatus
{
    Waiting,
    InProgress,
    Completed,
    Cancelled
}
