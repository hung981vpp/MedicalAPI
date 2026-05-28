using MedicalRecordService.Data;
using MedicalRecordService.DTOs;
using MedicalRecordService.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Services;

public sealed class LabOrderService(MedicalDbContext db)
{
    public LabOrder Create(int recordId, CreateLabOrderRequest request, int userId)
    {
        lock (db.Gate)
        {
            var record = db.MedicalRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Medical record not found.");
            EnsureDoctorHasLock(record, userId);

            var order = new LabOrder
            {
                Id = db.NextLabOrderId(),
                MedicalRecordId = record.Id,
                PatientId = record.PatientId,
                DoctorId = userId,
                OrderType = request.OrderType,
                ClinicalNote = request.ClinicalNote,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            foreach (var detail in request.Details)
            {
                order.Details.Add(new LabOrderDetail
                {
                    Id = db.NextLabDetailId(),
                    LabOrderId = order.Id,
                    TestName = detail.TestName,
                    Unit = detail.Unit,
                    ReferenceRange = detail.ReferenceRange
                });
            }

            db.LabOrders.Add(order);
            db.SaveChanges();
            return order;
        }
    }

    public List<LabOrder> GetByRecord(int recordId)
    {
        lock (db.Gate)
        {
            return [.. db.LabOrders.Include(x => x.Details).Where(x => x.MedicalRecordId == recordId).OrderByDescending(x => x.CreatedAt)];
        }
    }

    public LabOrder UpdateResults(int id, UpdateLabOrderResultsRequest request)
    {
        lock (db.Gate)
        {
            var order = db.LabOrders.Include(x => x.Details).FirstOrDefault(x => x.Id == id)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Lab order not found.");

            foreach (var result in request.Details)
            {
                var detail = order.Details.FirstOrDefault(x => x.Id == result.DetailId)
                    ?? throw new ApiException(StatusCodes.Status404NotFound, $"Lab order detail {result.DetailId} not found.");
                detail.ResultValue = result.ResultValue;
                detail.Conclusion = result.Conclusion;
            }

            order.Status = LabOrderStatus.Resulted;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            db.SaveChanges();
            return order;
        }
    }

    private void EnsureDoctorHasLock(MedicalRecord record, int userId)
    {
        if (record.Status == MedicalRecordStatus.Completed)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Benh an da Completed, khong duoc chi dinh xet nghiem.");
        }

        var lockRow = db.MedicalRecordLocks.FirstOrDefault(x => x.MedicalRecordId == record.Id && x.ExpiresAt > DateTimeOffset.UtcNow);
        if (lockRow is null || lockRow.LockedByUserId != userId)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Can giu lock hop le truoc khi chi dinh xet nghiem.");
        }
    }
}
