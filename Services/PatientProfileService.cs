using MedicalRecordService.Data;
using MedicalRecordService.DTOs;
using MedicalRecordService.Models;

namespace MedicalRecordService.Services;

public sealed class PatientProfileService(MedicalDbContext db)
{
    public List<PatientMedicalProfile> GetAll()
    {
        lock (db.Gate)
        {
            return [.. db.PatientMedicalProfiles.OrderBy(x => x.PatientName)];
        }
    }

    public PatientMedicalProfile? GetByPatientId(int patientId)
    {
        lock (db.Gate)
        {
            return db.PatientMedicalProfiles.FirstOrDefault(x => x.PatientId == patientId);
        }
    }

    public PatientMedicalProfile Upsert(UpsertPatientProfileRequest request)
    {
        if (request.PatientId <= 0 || string.IsNullOrWhiteSpace(request.PatientName))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "PatientId and PatientName are required.");
        }

        lock (db.Gate)
        {
            var now = DateTimeOffset.UtcNow;
            var profile = db.PatientMedicalProfiles.FirstOrDefault(x => x.PatientId == request.PatientId);
            if (profile is null)
            {
                profile = new PatientMedicalProfile
                {
                    PatientId = request.PatientId,
                    CreatedAt = now
                };
                db.PatientMedicalProfiles.Add(profile);
            }

            profile.PatientName = request.PatientName.Trim();
            profile.DateOfBirth = request.DateOfBirth;
            profile.BloodType = request.BloodType;
            profile.Allergies = request.Allergies;
            profile.ChronicDiseases = request.ChronicDiseases;
            profile.EmergencyContactName = request.EmergencyContactName;
            profile.EmergencyContactPhone = request.EmergencyContactPhone;
            profile.UpdatedAt = now;
            db.SaveChanges();
            return profile;
        }
    }
}
