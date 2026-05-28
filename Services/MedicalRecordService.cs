using System.Text.Json;
using MedicalRecordService.Data;
using MedicalRecordService.DTOs;
using MedicalRecordService.Models;

namespace MedicalRecordService.Services;

public sealed class MedicalRecordService(MedicalDbContext db)
{
    public List<MedicalRecord> GetAll(int? patientId = null)
    {
        lock (db.Gate)
        {
            var query = db.MedicalRecords.AsEnumerable();
            if (patientId.HasValue)
            {
                query = query.Where(x => x.PatientId == patientId.Value);
            }

            return [.. query.OrderByDescending(x => x.CreatedAt)];
        }
    }

    public MedicalRecord Get(int id)
    {
        lock (db.Gate)
        {
            return db.MedicalRecords.FirstOrDefault(x => x.Id == id)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Medical record not found.");
        }
    }

    public MedicalRecord Update(int id, UpdateMedicalRecordRequest request, int userId)
    {
        lock (db.Gate)
        {
            var record = Get(id);
            EnsureEditable(record, userId);

            var oldValue = JsonSerializer.Serialize(record);
            record.Symptoms = request.Symptoms;
            record.PhysicalExam = request.PhysicalExam;
            record.PreliminaryDiagnosis = request.PreliminaryDiagnosis;
            record.FinalDiagnosis = request.FinalDiagnosis;
            record.TreatmentPlan = request.TreatmentPlan;
            record.UpdatedAt = DateTimeOffset.UtcNow;
            AddLog(record.Id, userId, "medical_record.updated", oldValue, JsonSerializer.Serialize(record));
            db.SaveChanges();
            return record;
        }
    }

    public MedicalRecord Complete(int id, CompleteConsultationRequest request, int userId)
    {
        lock (db.Gate)
        {
            var record = Get(id);
            EnsureEditable(record, userId);

            var diagnosis = request.FinalDiagnosis ?? record.FinalDiagnosis;
            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Khong duoc hoan tat benh an neu thieu chan doan.");
            }

            var hasRequiredVital = db.VitalSigns.Any(x => x.PatientId == record.PatientId && x.Pulse > 0 && !string.IsNullOrWhiteSpace(x.BloodPressure));
            if (!hasRequiredVital)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Can co mach va huyet ap truoc khi hoan tat benh an.");
            }

            var oldValue = JsonSerializer.Serialize(record);
            record.FinalDiagnosis = diagnosis;
            record.TreatmentPlan = request.TreatmentPlan ?? record.TreatmentPlan;
            record.Status = MedicalRecordStatus.Completed;
            record.CompletedAt = DateTimeOffset.UtcNow;
            record.UpdatedAt = record.CompletedAt.Value;
            var locks = db.MedicalRecordLocks.Where(x => x.MedicalRecordId == id && x.LockedByUserId == userId).ToList();
            db.MedicalRecordLocks.RemoveRange(locks);
            AddLog(record.Id, userId, "medical_record.completed", oldValue, JsonSerializer.Serialize(record));
            db.SaveChanges();
            return record;
        }
    }

    public ReferralLetter CreateReferral(int recordId, CreateReferralRequest request, int userId)
    {
        lock (db.Gate)
        {
            var record = Get(recordId);
            EnsureEditable(record, userId);

            var referral = new ReferralLetter
            {
                Id = db.NextReferralId(),
                MedicalRecordId = record.Id,
                PatientId = record.PatientId,
                DoctorId = userId,
                TargetHospital = request.TargetHospital,
                Reason = request.Reason,
                ClinicalSummary = request.ClinicalSummary,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.ReferralLetters.Add(referral);
            AddLog(record.Id, userId, "referral.created", null, JsonSerializer.Serialize(referral));
            db.SaveChanges();
            return referral;
        }
    }

    public List<ReferralLetter> GetReferrals(int recordId)
    {
        lock (db.Gate)
        {
            return [.. db.ReferralLetters.Where(x => x.MedicalRecordId == recordId).OrderByDescending(x => x.CreatedAt)];
        }
    }

    public List<MedicalRecordLog> GetLogs(int? recordId)
    {
        lock (db.Gate)
        {
            var query = db.MedicalRecordLogs.AsEnumerable();
            if (recordId.HasValue)
            {
                query = query.Where(x => x.MedicalRecordId == recordId.Value);
            }

            return [.. query.OrderByDescending(x => x.CreatedAt)];
        }
    }

    private void EnsureEditable(MedicalRecord record, int userId)
    {
        if (record.Status == MedicalRecordStatus.Completed)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Benh an da Completed, khong duoc chinh sua.");
        }

        var lockRow = db.MedicalRecordLocks.FirstOrDefault(x => x.MedicalRecordId == record.Id && x.ExpiresAt > DateTimeOffset.UtcNow);
        if (lockRow is null || lockRow.LockedByUserId != userId)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Can giu lock hop le truoc khi chinh sua benh an.");
        }
    }

    private void AddLog(int recordId, int userId, string action, string? oldValueJson, string? newValueJson)
    {
        db.MedicalRecordLogs.Add(new MedicalRecordLog
        {
            Id = db.NextLogId(),
            MedicalRecordId = recordId,
            UserId = userId,
            Action = action,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
