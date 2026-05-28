using MedicalRecordService.Data;
using MedicalRecordService.DTOs;
using MedicalRecordService.Models;

namespace MedicalRecordService.Services;

public sealed class ConsultationService(MedicalDbContext db)
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public ActiveConsultationSession Start(StartConsultationRequest request)
    {
        lock (db.Gate)
        {
            var active = db.ActiveConsultationSessions.Any(x =>
                x.Status == ConsultationStatus.Active &&
                (x.AppointmentId == request.AppointmentId || x.PatientId == request.PatientId));

            if (active)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Ca kham nay dang duoc thuc hien o mot phien khac.");
            }

            var now = DateTimeOffset.UtcNow;
            var record = db.MedicalRecords.FirstOrDefault(x => x.AppointmentId == request.AppointmentId);
            if (record is null)
            {
                record = new MedicalRecord
                {
                    AppointmentId = request.AppointmentId,
                    PatientId = request.PatientId,
                    DoctorId = request.DoctorId,
                    Status = MedicalRecordStatus.Draft,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.MedicalRecords.Add(record);
                db.SaveChanges();
            }

            var session = new ActiveConsultationSession
            {
                AppointmentId = request.AppointmentId,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                MedicalRecordId = record.Id,
                StartedAt = now
            };
            db.ActiveConsultationSessions.Add(session);
            AcquireLockCore(record.Id, request.DoctorId, now);

            var queue = db.LocalExamQueue.FirstOrDefault(x => x.AppointmentId == request.AppointmentId);
            if (queue is not null)
            {
                queue.Status = QueueStatus.InProgress;
            }

            db.SaveChanges();
            return session;
        }
    }

    public MedicalRecordLock AcquireLock(int medicalRecordId, int userId)
    {
        lock (db.Gate)
        {
            var lockRow = AcquireLockCore(medicalRecordId, userId, DateTimeOffset.UtcNow);
            db.SaveChanges();
            return lockRow;
        }
    }

    public MedicalRecordLock RefreshLock(int medicalRecordId, int userId)
    {
        lock (db.Gate)
        {
            var lockRow = db.MedicalRecordLocks.FirstOrDefault(x => x.MedicalRecordId == medicalRecordId);
            if (lockRow is null || lockRow.LockedByUserId != userId)
            {
                throw new ApiException(StatusCodes.Status409Conflict, "Ban khong giu khoa benh an nay.");
            }

            lockRow.ExpiresAt = DateTimeOffset.UtcNow.Add(LockDuration);
            db.SaveChanges();
            return lockRow;
        }
    }

    public void ReleaseLock(int medicalRecordId, int userId)
    {
        lock (db.Gate)
        {
            var locks = db.MedicalRecordLocks
                .Where(x => x.MedicalRecordId == medicalRecordId && x.LockedByUserId == userId)
                .ToList();
            db.MedicalRecordLocks.RemoveRange(locks);
            db.SaveChanges();
        }
    }

    public ActiveConsultationSession End(int sessionId, int userId, bool cancel)
    {
        lock (db.Gate)
        {
            var session = db.ActiveConsultationSessions.FirstOrDefault(x => x.Id == sessionId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Consultation session not found.");

            if (session.DoctorId != userId)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "Chi bac si dang kham moi duoc ket thuc phien.");
            }

            session.Status = cancel ? ConsultationStatus.Cancelled : ConsultationStatus.Completed;
            session.EndedAt = DateTimeOffset.UtcNow;
            ReleaseLock(session.MedicalRecordId, userId);

            var queue = db.LocalExamQueue.FirstOrDefault(x => x.AppointmentId == session.AppointmentId);
            if (queue is not null)
            {
                queue.Status = cancel ? QueueStatus.Cancelled : QueueStatus.Completed;
            }

            db.SaveChanges();
            return session;
        }
    }

    private MedicalRecordLock AcquireLockCore(int medicalRecordId, int userId, DateTimeOffset now)
    {
        var record = db.MedicalRecords.FirstOrDefault(x => x.Id == medicalRecordId)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Medical record not found.");

        if (record.Status is MedicalRecordStatus.Completed or MedicalRecordStatus.Cancelled)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Benh an da dong, khong duoc chinh sua.");
        }

        var expiredLocks = db.MedicalRecordLocks.Where(x => x.ExpiresAt <= now).ToList();
        db.MedicalRecordLocks.RemoveRange(expiredLocks);
        var existing = db.MedicalRecordLocks.FirstOrDefault(x => x.MedicalRecordId == medicalRecordId);
        if (existing is not null && existing.LockedByUserId != userId)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Benh an dang bi khoa boi nguoi dung khac.");
        }

        if (existing is null)
        {
            existing = new MedicalRecordLock
            {
                MedicalRecordId = medicalRecordId,
                LockedByUserId = userId,
                LockedAt = now
            };
            db.MedicalRecordLocks.Add(existing);
        }

        existing.ExpiresAt = now.Add(LockDuration);
        record.Status = MedicalRecordStatus.Locked;
        record.UpdatedAt = now;
        return existing;
    }
}
