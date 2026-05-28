using MedicalRecordService.Data;
using MedicalRecordService.DTOs;
using MedicalRecordService.Models;

namespace MedicalRecordService.Services;

public sealed class VitalSignService(MedicalDbContext db)
{
    public List<VitalSign> GetByPatient(int patientId)
    {
        lock (db.Gate)
        {
            return [.. db.VitalSigns.Where(x => x.PatientId == patientId).OrderByDescending(x => x.RecordedAt)];
        }
    }

    public VitalSign Create(CreateVitalSignRequest request, int userId)
    {
        if (request.PatientId <= 0 || request.Pulse <= 0 || string.IsNullOrWhiteSpace(request.BloodPressure))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "PatientId, Pulse and BloodPressure are required.");
        }

        lock (db.Gate)
        {
            var vital = new VitalSign
            {
                PatientId = request.PatientId,
                AppointmentId = request.AppointmentId,
                MedicalRecordId = request.MedicalRecordId,
                RecordedByUserId = userId,
                Pulse = request.Pulse,
                BloodPressure = request.BloodPressure.Trim(),
                TemperatureC = request.TemperatureC,
                Spo2 = request.Spo2,
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                Note = request.Note,
                RecordedAt = DateTimeOffset.UtcNow
            };
            db.VitalSigns.Add(vital);
            db.SaveChanges();
            return vital;
        }
    }
}
