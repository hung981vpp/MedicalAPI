using MedicalRecordService.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Data;

public sealed class MedicalDbContext(DbContextOptions<MedicalDbContext> options) : DbContext(options)
{
    public object Gate { get; } = new();

    public DbSet<PatientMedicalProfile> PatientMedicalProfiles => Set<PatientMedicalProfile>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<ActiveConsultationSession> ActiveConsultationSessions => Set<ActiveConsultationSession>();
    public DbSet<MedicalRecordLock> MedicalRecordLocks => Set<MedicalRecordLock>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabOrderDetail> LabOrderDetails => Set<LabOrderDetail>();
    public DbSet<LocalMedicine> LocalMedicines => Set<LocalMedicine>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<ReferralLetter> ReferralLetters => Set<ReferralLetter>();
    public DbSet<LocalExamQueue> LocalExamQueue => Set<LocalExamQueue>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<MedicalRecordLog> MedicalRecordLogs => Set<MedicalRecordLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientMedicalProfile>()
            .HasIndex(x => x.PatientId)
            .IsUnique();

        modelBuilder.Entity<LocalMedicine>()
            .HasKey(x => x.MedicineId);

        modelBuilder.Entity<LocalExamQueue>()
            .HasKey(x => x.AppointmentId);

        modelBuilder.Entity<MedicalRecord>()
            .HasIndex(x => x.AppointmentId)
            .IsUnique();

        modelBuilder.Entity<MedicalRecordLock>()
            .HasIndex(x => x.MedicalRecordId)
            .IsUnique();

        modelBuilder.Entity<ActiveConsultationSession>()
            .HasIndex(x => new { x.AppointmentId, x.Status });

        modelBuilder.Entity<LabOrder>()
            .HasMany(x => x.Details)
            .WithOne()
            .HasForeignKey(x => x.LabOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Prescription>()
            .HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecord>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<ActiveConsultationSession>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<LabOrder>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<Prescription>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<Prescription>()
            .Property(x => x.PrescriptionType)
            .HasConversion<string>();
        modelBuilder.Entity<LocalExamQueue>()
            .Property(x => x.Status)
            .HasConversion<string>();
    }

    public int NextProfileId() => NextId(PatientMedicalProfiles);
    public int NextVitalSignId() => NextId(VitalSigns);
    public int NextRecordId() => NextId(MedicalRecords);
    public int NextSessionId() => NextId(ActiveConsultationSessions);
    public int NextLockId() => NextId(MedicalRecordLocks);
    public int NextLabOrderId() => NextId(LabOrders);
    public int NextLabDetailId() => NextId(LabOrderDetails);
    public int NextPrescriptionId() => NextId(Prescriptions);
    public int NextPrescriptionItemId() => NextId(PrescriptionItems);
    public int NextReferralId() => NextId(ReferralLetters);
    public int NextLogId() => NextId(MedicalRecordLogs);

    private static int NextId<T>(DbSet<T> set) where T : class
    {
        var localMax = set.Local
            .Select(x => (int?)typeof(T).GetProperty("Id")?.GetValue(x))
            .Max() ?? 0;
        var dbMax = set.AsEnumerable()
            .Select(x => (int?)typeof(T).GetProperty("Id")?.GetValue(x))
            .Max() ?? 0;
        return Math.Max(localMax, dbMax) + 1;
    }
}
