namespace MedicalRecordService.Models;

public sealed class PatientMedicalProfile
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class VitalSign
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public int? MedicalRecordId { get; set; }
    public int RecordedByUserId { get; set; }
    public int Pulse { get; set; }
    public string BloodPressure { get; set; } = string.Empty;
    public decimal? TemperatureC { get; set; }
    public int? Spo2 { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class MedicalRecord
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int AppointmentId { get; set; }
    public string? Symptoms { get; set; }
    public string? PhysicalExam { get; set; }
    public string? PreliminaryDiagnosis { get; set; }
    public string? FinalDiagnosis { get; set; }
    public string? TreatmentPlan { get; set; }
    public MedicalRecordStatus Status { get; set; } = MedicalRecordStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class ActiveConsultationSession
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int MedicalRecordId { get; set; }
    public ConsultationStatus Status { get; set; } = ConsultationStatus.Active;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}

public sealed class MedicalRecordLock
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int LockedByUserId { get; set; }
    public DateTimeOffset LockedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class LabOrder
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string ClinicalNote { get; set; } = string.Empty;
    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;
    public List<LabOrderDetail> Details { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class LabOrderDetail
{
    public int Id { get; set; }
    public int LabOrderId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Conclusion { get; set; }
}

public sealed class LocalMedicine
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? ActiveIngredient { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset SyncedAt { get; set; }
}

public sealed class Prescription
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int AppointmentId { get; set; }
    public PrescriptionType PrescriptionType { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public List<PrescriptionItem> Items { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
}

public sealed class PrescriptionItem
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public string? Instruction { get; set; }
    public bool AllergyOverrideConfirmed { get; set; }
}

public sealed class ReferralLetter
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string TargetHospital { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ClinicalSummary { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class LocalExamQueue
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateOnly? PatientDob { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public decimal ExamFee { get; set; }
    public string QueueNumber { get; set; } = string.Empty;
    public QueueStatus Status { get; set; } = QueueStatus.Waiting;
    public DateTimeOffset CheckedInAt { get; set; }
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public sealed class MedicalRecordLog
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
