using MedicalRecordService.Models;

namespace MedicalRecordService.DTOs;

public sealed record UpsertPatientProfileRequest(
    int PatientId,
    string PatientName,
    DateOnly? DateOfBirth,
    string? BloodType,
    string? Allergies,
    string? ChronicDiseases,
    string? EmergencyContactName,
    string? EmergencyContactPhone);

public sealed record CreateVitalSignRequest(
    int PatientId,
    int? AppointmentId,
    int? MedicalRecordId,
    int Pulse,
    string BloodPressure,
    decimal? TemperatureC,
    int? Spo2,
    decimal? HeightCm,
    decimal? WeightKg,
    string? Note);

public sealed record StartConsultationRequest(int AppointmentId, int PatientId, int DoctorId);

public sealed record CompleteConsultationRequest(string? FinalDiagnosis, string? TreatmentPlan);

public sealed record UpdateMedicalRecordRequest(
    string? Symptoms,
    string? PhysicalExam,
    string? PreliminaryDiagnosis,
    string? FinalDiagnosis,
    string? TreatmentPlan);

public sealed record CreateLabOrderRequest(string OrderType, string ClinicalNote, List<CreateLabOrderDetailRequest> Details);

public sealed record CreateLabOrderDetailRequest(string TestName, string? Unit, string? ReferenceRange);

public sealed record UpdateLabOrderResultsRequest(List<UpdateLabOrderDetailRequest> Details);

public sealed record UpdateLabOrderDetailRequest(int DetailId, string? ResultValue, string? Conclusion);

public sealed record CreatePrescriptionRequest(PrescriptionType PrescriptionType, List<CreatePrescriptionItemRequest> Items);

public sealed record CreatePrescriptionItemRequest(
    int MedicineId,
    int Quantity,
    string Dosage,
    string Frequency,
    int DurationDays,
    string? Instruction,
    string? AllergyOverrideCode);

public sealed record CreateReferralRequest(string TargetHospital, string Reason, string ClinicalSummary);

public sealed record AppointmentCheckedInEvent(
    Guid EventId,
    string EventName,
    DateTimeOffset Timestamp,
    int AppointmentId,
    int PatientId,
    string PatientName,
    DateOnly? PatientDob,
    int DoctorId,
    string DoctorName,
    string Specialty,
    decimal ExamFee,
    string QueueNumber);

public sealed record MedicineSyncedEvent(
    Guid EventId,
    string EventName,
    DateTimeOffset Timestamp,
    int MedicineId,
    string MedicineName,
    string? ActiveIngredient,
    string Unit,
    decimal Price,
    bool IsActive);
