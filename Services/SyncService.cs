using MedicalRecordService.Data;
using MedicalRecordService.DTOs;
using MedicalRecordService.Models;

namespace MedicalRecordService.Services;

public sealed class SyncService(MedicalDbContext db)
{
    public LocalExamQueue UpsertAppointment(AppointmentCheckedInEvent evt)
    {
        lock (db.Gate)
        {
            var queue = db.LocalExamQueue.FirstOrDefault(x => x.AppointmentId == evt.AppointmentId);
            if (queue is null)
            {
                queue = new LocalExamQueue { AppointmentId = evt.AppointmentId };
                db.LocalExamQueue.Add(queue);
            }

            queue.PatientId = evt.PatientId;
            queue.PatientName = evt.PatientName;
            queue.PatientDob = evt.PatientDob;
            queue.DoctorId = evt.DoctorId;
            queue.DoctorName = evt.DoctorName;
            queue.Specialty = evt.Specialty;
            queue.ExamFee = evt.ExamFee;
            queue.QueueNumber = evt.QueueNumber;
            queue.CheckedInAt = evt.Timestamp;
            if (queue.Status == QueueStatus.Cancelled)
            {
                queue.Status = QueueStatus.Waiting;
            }

            db.SaveChanges();
            return queue;
        }
    }

    public LocalMedicine UpsertMedicine(MedicineSyncedEvent evt)
    {
        lock (db.Gate)
        {
            var medicine = db.LocalMedicines.FirstOrDefault(x => x.MedicineId == evt.MedicineId);
            if (medicine is null)
            {
                medicine = new LocalMedicine { MedicineId = evt.MedicineId };
                db.LocalMedicines.Add(medicine);
            }

            medicine.MedicineName = evt.MedicineName;
            medicine.ActiveIngredient = evt.ActiveIngredient;
            medicine.Unit = evt.Unit;
            medicine.Price = evt.Price;
            medicine.IsActive = evt.IsActive;
            medicine.SyncedAt = evt.Timestamp;
            db.SaveChanges();
            return medicine;
        }
    }

    public List<LocalExamQueue> GetQueue(int? doctorId)
    {
        lock (db.Gate)
        {
            var query = db.LocalExamQueue.AsEnumerable();
            if (doctorId.HasValue)
            {
                query = query.Where(x => x.DoctorId == doctorId);
            }

            return [.. query.OrderBy(x => x.QueueNumber)];
        }
    }

    public List<LocalMedicine> GetMedicines(string? q)
    {
        lock (db.Gate)
        {
            var query = db.LocalMedicines.Where(x => x.IsActive).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x =>
                    x.MedicineName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.ActiveIngredient?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return [.. query.OrderBy(x => x.MedicineName)];
        }
    }
}
