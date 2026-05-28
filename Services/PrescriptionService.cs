using System.Text.Json;
using MedicalRecordService.Data;
using MedicalRecordService.DTOs;
using MedicalRecordService.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Services;

public sealed class PrescriptionService(MedicalDbContext db)
{
    private const string AllergyOverrideCode = "CONFIRM_ALLERGY_OVERRIDE";

    public Prescription Create(int recordId, CreatePrescriptionRequest request, int userId)
    {
        if (request.Items.Count == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Prescription must contain at least one medicine.");
        }

        lock (db.Gate)
        {
            var record = db.MedicalRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Medical record not found.");
            EnsureDoctorHasLock(record, userId);

            var profile = db.PatientMedicalProfiles.FirstOrDefault(x => x.PatientId == record.PatientId);
            var allergyText = profile?.Allergies ?? string.Empty;
            var prescription = new Prescription
            {
                MedicalRecordId = record.Id,
                PatientId = record.PatientId,
                DoctorId = userId,
                AppointmentId = record.AppointmentId,
                PrescriptionType = request.PrescriptionType,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            foreach (var itemRequest in request.Items)
            {
                var medicine = db.LocalMedicines.FirstOrDefault(x => x.MedicineId == itemRequest.MedicineId && x.IsActive)
                    ?? throw new ApiException(StatusCodes.Status404NotFound, $"Local medicine {itemRequest.MedicineId} not found.");

                var allergen = medicine.ActiveIngredient ?? medicine.MedicineName;
                var hasAllergy = !string.IsNullOrWhiteSpace(allergyText) &&
                    allergyText.Contains(allergen, StringComparison.OrdinalIgnoreCase);
                var overrideConfirmed = itemRequest.AllergyOverrideCode == AllergyOverrideCode;
                if (hasAllergy && !overrideConfirmed)
                {
                    throw new ApiException(StatusCodes.Status409Conflict,
                        $"Canh bao di ung: benh nhan co di ung voi {allergen}. Gui AllergyOverrideCode='{AllergyOverrideCode}' neu bac si chap nhan ghi de.");
                }

                prescription.Items.Add(new PrescriptionItem
                {
                    MedicineId = medicine.MedicineId,
                    MedicineName = medicine.MedicineName,
                    Quantity = itemRequest.Quantity,
                    Dosage = itemRequest.Dosage,
                    Frequency = itemRequest.Frequency,
                    DurationDays = itemRequest.DurationDays,
                    Instruction = itemRequest.Instruction,
                    AllergyOverrideConfirmed = overrideConfirmed
                });
            }

            db.Prescriptions.Add(prescription);
            db.SaveChanges();
            return prescription;
        }
    }

    public Prescription Get(int id)
    {
        lock (db.Gate)
        {
            return db.Prescriptions.Include(x => x.Items).FirstOrDefault(x => x.Id == id)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Prescription not found.");
        }
    }

    public Prescription Update(int id, CreatePrescriptionRequest request, int userId)
    {
        lock (db.Gate)
        {
            var existing = Get(id);
            if (existing.Status != PrescriptionStatus.Draft)
            {
                throw new ApiException(StatusCodes.Status409Conflict, "Chi duoc sua don thuoc o trang thai Draft.");
            }

            db.Prescriptions.Remove(existing);
            db.SaveChanges();
            return Create(existing.MedicalRecordId, request, userId);
        }
    }

    public Prescription Finalize(int id, int userId)
    {
        lock (db.Gate)
        {
            var prescription = Get(id);
            if (prescription.DoctorId != userId)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "Chi bac si tao don moi duoc hoan tat don.");
            }

            if (prescription.PrescriptionType is not PrescriptionType.Insurance and not PrescriptionType.Service)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "PrescriptionType is required.");
            }

            if (prescription.Status == PrescriptionStatus.Finalized)
            {
                return prescription;
            }

            prescription.Status = PrescriptionStatus.Finalized;
            prescription.FinalizedAt = DateTimeOffset.UtcNow;
            prescription.UpdatedAt = prescription.FinalizedAt.Value;

            var payload = new
            {
                EventId = Guid.NewGuid(),
                EventName = "prescription.created",
                Timestamp = DateTimeOffset.UtcNow,
                PrescriptionId = prescription.Id,
                prescription.MedicalRecordId,
                prescription.PatientId,
                prescription.DoctorId,
                prescription.AppointmentId,
                PrescriptionType = prescription.PrescriptionType.ToString(),
                Items = prescription.Items.Select(x => new
                {
                    x.MedicineId,
                    x.MedicineName,
                    x.Quantity,
                    x.Dosage,
                    x.Frequency,
                    x.DurationDays,
                    x.Instruction
                })
            };

            db.OutboxMessages.Add(new OutboxMessage
            {
                EventName = "prescription.created",
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAt = DateTimeOffset.UtcNow
            });

            db.SaveChanges();
            return prescription;
        }
    }

    private void EnsureDoctorHasLock(MedicalRecord record, int userId)
    {
        if (record.Status == MedicalRecordStatus.Completed)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Benh an da Completed, khong duoc ke don.");
        }

        var lockRow = db.MedicalRecordLocks.FirstOrDefault(x => x.MedicalRecordId == record.Id && x.ExpiresAt > DateTimeOffset.UtcNow);
        if (lockRow is null || lockRow.LockedByUserId != userId)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Can giu lock hop le truoc khi ke don.");
        }
    }
}
