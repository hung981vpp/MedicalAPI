using MedicalRecordService.Data;
using MedicalRecordService.Helpers;
using MedicalRecordService.Models;
using MedicalRecordService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "MedicalRecordService-DataProtectionKeys")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<AuthHeadersOperationFilter>();
});

builder.Services
    .AddAuthentication("HeaderAuth")
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("HeaderAuth", _ => { });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<MedicalDbContext>(options =>
    options.UseNpgsql(BuildPostgresConnectionString(builder.Configuration)));
builder.Services.AddScoped<PatientProfileService>();
builder.Services.AddScoped<VitalSignService>();
builder.Services.AddScoped<ConsultationService>();
builder.Services.AddScoped<MedicalRecordService.Services.MedicalRecordService>();
builder.Services.AddScoped<LabOrderService>();
builder.Services.AddScoped<PrescriptionService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddHostedService<OutboxProcessorWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();
    db.Database.EnsureCreated();
    SeedDemoData(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception is ApiException apiException)
        {
            context.Response.StatusCode = apiException.StatusCode;
            await context.Response.WriteAsJsonAsync(new { error = apiException.Message });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Unexpected server error." });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string BuildPostgresConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    return configuration.GetConnectionString("MedicalDB")
        ?? "Host=localhost;Port=5432;Database=MedicalDB;Username=postgres;Password=postgres";
}

static void SeedDemoData(MedicalDbContext db)
{
    lock (db.Gate)
    {
        var now = DateTimeOffset.UtcNow;

        if (!db.LocalMedicines.Any())
        {
            db.LocalMedicines.AddRange([
                new LocalMedicine
                {
                    MedicineId = 1,
                    MedicineName = "Paracetamol 500mg",
                    ActiveIngredient = "Paracetamol",
                    Unit = "Vien",
                    Price = 1200,
                    IsActive = true,
                    SyncedAt = now
                },
                new LocalMedicine
                {
                    MedicineId = 2,
                    MedicineName = "Amoxicillin 500mg",
                    ActiveIngredient = "Penicillin",
                    Unit = "Vien",
                    Price = 2500,
                    IsActive = true,
                    SyncedAt = now
                },
                new LocalMedicine
                {
                    MedicineId = 3,
                    MedicineName = "Loratadine 10mg",
                    ActiveIngredient = "Loratadine",
                    Unit = "Vien",
                    Price = 1800,
                    IsActive = true,
                    SyncedAt = now
                },
                new LocalMedicine
                {
                    MedicineId = 4,
                    MedicineName = "Omeprazole 20mg",
                    ActiveIngredient = "Omeprazole",
                    Unit = "Vien",
                    Price = 2200,
                    IsActive = true,
                    SyncedAt = now
                },
                new LocalMedicine
                {
                    MedicineId = 5,
                    MedicineName = "Salbutamol inhaler",
                    ActiveIngredient = "Salbutamol",
                    Unit = "Binh",
                    Price = 78000,
                    IsActive = true,
                    SyncedAt = now
                },
                new LocalMedicine
                {
                    MedicineId = 6,
                    MedicineName = "Amlodipine 5mg",
                    ActiveIngredient = "Amlodipine",
                    Unit = "Vien",
                    Price = 1600,
                    IsActive = true,
                    SyncedAt = now
                },
                new LocalMedicine
                {
                    MedicineId = 7,
                    MedicineName = "Metformin 500mg",
                    ActiveIngredient = "Metformin",
                    Unit = "Vien",
                    Price = 1300,
                    IsActive = true,
                    SyncedAt = now
                },
                new LocalMedicine
                {
                    MedicineId = 8,
                    MedicineName = "Cefixime 200mg",
                    ActiveIngredient = "Cephalosporin",
                    Unit = "Vien",
                    Price = 8500,
                    IsActive = true,
                    SyncedAt = now
                }]);
        }

        if (!db.LocalExamQueue.Any())
        {
            db.LocalExamQueue.AddRange([
                new LocalExamQueue
                {
                    AppointmentId = 3001,
                    PatientId = 25,
                    PatientName = "Nguyen Van An",
                    PatientDob = new DateOnly(2002, 5, 12),
                    DoctorId = 7,
                    DoctorName = "BS. Tran Quoc Anh",
                    Specialty = "Khoa Noi",
                    ExamFee = 150000,
                    QueueNumber = "N-05",
                    CheckedInAt = now.AddMinutes(-35)
                },
                new LocalExamQueue
                {
                    AppointmentId = 3002,
                    PatientId = 26,
                    PatientName = "Le Thi Mai",
                    PatientDob = new DateOnly(1988, 9, 21),
                    DoctorId = 7,
                    DoctorName = "BS. Tran Quoc Anh",
                    Specialty = "Khoa Noi",
                    ExamFee = 150000,
                    QueueNumber = "N-06",
                    CheckedInAt = now.AddMinutes(-22)
                },
                new LocalExamQueue
                {
                    AppointmentId = 3003,
                    PatientId = 27,
                    PatientName = "Pham Minh Khoa",
                    PatientDob = new DateOnly(2015, 3, 2),
                    DoctorId = 8,
                    DoctorName = "BS. Do My Linh",
                    Specialty = "Nhi khoa",
                    ExamFee = 120000,
                    QueueNumber = "P-03",
                    CheckedInAt = now.AddMinutes(-15)
                },
                new LocalExamQueue
                {
                    AppointmentId = 3004,
                    PatientId = 28,
                    PatientName = "Tran Van Binh",
                    PatientDob = new DateOnly(1959, 12, 4),
                    DoctorId = 9,
                    DoctorName = "BS. Hoang Nam",
                    Specialty = "Tim mach",
                    ExamFee = 200000,
                    QueueNumber = "T-02",
                    CheckedInAt = now.AddMinutes(-8)
                }]);
        }

        if (!db.PatientMedicalProfiles.Any())
        {
            db.PatientMedicalProfiles.AddRange([
                new PatientMedicalProfile
                {
                    PatientId = 25,
                    PatientName = "Nguyen Van An",
                    DateOfBirth = new DateOnly(2002, 5, 12),
                    BloodType = "O+",
                    Allergies = "Penicillin",
                    ChronicDiseases = "Viêm mũi dị ứng theo mùa, hay mất ngủ khi căng thẳng",
                    EmergencyContactName = "Nguyen Thi Hoa - me",
                    EmergencyContactPhone = "0901000001",
                    CreatedAt = now.AddMonths(-7),
                    UpdatedAt = now.AddDays(-2)
                },
                new PatientMedicalProfile
                {
                    PatientId = 26,
                    PatientName = "Le Thi Mai",
                    DateOfBirth = new DateOnly(1988, 9, 21),
                    BloodType = "A+",
                    Allergies = "Khong ghi nhan",
                    ChronicDiseases = "Dau da day tai phat, lam viec van phong, thuong bo bua sang",
                    EmergencyContactName = "Le Quang Huy - chong",
                    EmergencyContactPhone = "0901000002",
                    CreatedAt = now.AddMonths(-3),
                    UpdatedAt = now.AddDays(-1)
                },
                new PatientMedicalProfile
                {
                    PatientId = 27,
                    PatientName = "Pham Minh Khoa",
                    DateOfBirth = new DateOnly(2015, 3, 2),
                    BloodType = "B+",
                    Allergies = "Hai san",
                    ChronicDiseases = "Hen phe quan nhe, can theo doi khi thay doi thoi tiet",
                    EmergencyContactName = "Pham Thi Lan - me",
                    EmergencyContactPhone = "0901000003",
                    CreatedAt = now.AddMonths(-11),
                    UpdatedAt = now.AddDays(-10)
                },
                new PatientMedicalProfile
                {
                    PatientId = 28,
                    PatientName = "Tran Van Binh",
                    DateOfBirth = new DateOnly(1959, 12, 4),
                    BloodType = "AB+",
                    Allergies = "Aspirin",
                    ChronicDiseases = "Tang huyet ap 8 nam, dai thao duong type 2",
                    EmergencyContactName = "Tran Minh Duc - con trai",
                    EmergencyContactPhone = "0901000004",
                    CreatedAt = now.AddYears(-1),
                    UpdatedAt = now.AddDays(-5)
                },
                new PatientMedicalProfile
                {
                    PatientId = 29,
                    PatientName = "Hoang Thu Ha",
                    DateOfBirth = new DateOnly(1996, 6, 18),
                    BloodType = "O-",
                    Allergies = "Cephalosporin",
                    ChronicDiseases = "Dang mang thai tuan 18, can uu tien thuoc an toan thai ky",
                    EmergencyContactName = "Hoang Van Son - anh trai",
                    EmergencyContactPhone = "0901000005",
                    CreatedAt = now.AddMonths(-2),
                    UpdatedAt = now.AddDays(-7)
                }]);
        }

        if (!db.VitalSigns.Any())
        {
            db.VitalSigns.AddRange([
                new VitalSign
                {
                    PatientId = 25,
                    AppointmentId = 3001,
                    RecordedByUserId = 12,
                    Pulse = 82,
                    BloodPressure = "118/76",
                    TemperatureC = 37.2m,
                    Spo2 = 98,
                    HeightCm = 172,
                    WeightKg = 64,
                    Note = "Benh nhan tinh tao, ho khan, met sau 2 ngay mat ngu.",
                    RecordedAt = now.AddMinutes(-30)
                },
                new VitalSign
                {
                    PatientId = 26,
                    AppointmentId = 3002,
                    RecordedByUserId = 12,
                    Pulse = 88,
                    BloodPressure = "110/70",
                    TemperatureC = 36.8m,
                    Spo2 = 99,
                    HeightCm = 158,
                    WeightKg = 51,
                    Note = "Dau thuong vi am i, khong non, an kem tu sang.",
                    RecordedAt = now.AddMinutes(-18)
                },
                new VitalSign
                {
                    PatientId = 27,
                    AppointmentId = 3003,
                    RecordedByUserId = 13,
                    Pulse = 104,
                    BloodPressure = "100/65",
                    TemperatureC = 38.1m,
                    Spo2 = 96,
                    HeightCm = 137,
                    WeightKg = 31,
                    Note = "Tre sot, kho khe nhe, me di cung va mang so kham cu.",
                    RecordedAt = now.AddMinutes(-12)
                },
                new VitalSign
                {
                    PatientId = 28,
                    AppointmentId = 3004,
                    RecordedByUserId = 12,
                    Pulse = 92,
                    BloodPressure = "154/92",
                    TemperatureC = 36.7m,
                    Spo2 = 97,
                    HeightCm = 166,
                    WeightKg = 72,
                    Note = "Than tuc nguc nhe khi leo cau thang, da uong thuoc huyet ap sang nay.",
                    RecordedAt = now.AddMinutes(-6)
                }]);
        }

        if (!db.MedicalRecords.Any())
        {
            db.MedicalRecords.AddRange([
                new MedicalRecord
                {
                    PatientId = 25,
                    DoctorId = 7,
                    AppointmentId = 2901,
                    Symptoms = "Ho khan, ngat mui, dau hong nhe sau khi di mua ve gap mua.",
                    PhysicalExam = "Hong do nhe, phoi thong khi tot, khong ran.",
                    PreliminaryDiagnosis = "Viem hong cap do virus",
                    FinalDiagnosis = "Viem hong cap, theo doi di ung thoi tiet",
                    TreatmentPlan = "Uong nhieu nuoc, nghi ngoi, tai kham neu sot cao hoac kho tho.",
                    Status = MedicalRecordStatus.Completed,
                    CreatedAt = now.AddDays(-18),
                    UpdatedAt = now.AddDays(-18).AddMinutes(25),
                    CompletedAt = now.AddDays(-18).AddMinutes(25)
                },
                new MedicalRecord
                {
                    PatientId = 28,
                    DoctorId = 9,
                    AppointmentId = 2808,
                    Symptoms = "Dau dau vung cham, hoi chong mat khi dung len nhanh.",
                    PhysicalExam = "Huyet ap cao hon muc muc tieu, tim deu, khong phu chan.",
                    PreliminaryDiagnosis = "Tang huyet ap chua kiem soat tot",
                    FinalDiagnosis = "Tang huyet ap do bo lieu va an man",
                    TreatmentPlan = "Tu van giam muoi, theo doi huyet ap tai nha, dieu chinh thuoc.",
                    Status = MedicalRecordStatus.Completed,
                    CreatedAt = now.AddDays(-34),
                    UpdatedAt = now.AddDays(-34).AddMinutes(40),
                    CompletedAt = now.AddDays(-34).AddMinutes(40)
                }]);
        }

        if (!db.LabOrders.Any())
        {
            db.LabOrders.Add(new LabOrder
            {
                MedicalRecordId = 2,
                PatientId = 28,
                DoctorId = 9,
                OrderType = "Xet nghiem mau",
                ClinicalNote = "Danh gia nguy co tim mach va duong huyet dinh ky.",
                Status = LabOrderStatus.Resulted,
                CreatedAt = now.AddDays(-34).AddMinutes(10),
                UpdatedAt = now.AddDays(-34).AddMinutes(35),
                Details =
                [
                    new LabOrderDetail
                    {
                        LabOrderId = 1,
                        TestName = "Glucose doi",
                        ResultValue = "8.4",
                        Unit = "mmol/L",
                        ReferenceRange = "3.9 - 5.6",
                        Conclusion = "Cao, can kiem soat che do an va thuoc."
                    },
                    new LabOrderDetail
                    {
                        LabOrderId = 1,
                        TestName = "Creatinine",
                        ResultValue = "89",
                        Unit = "umol/L",
                        ReferenceRange = "62 - 106",
                        Conclusion = "Trong gioi han."
                    }
                ]
            });
        }

        if (!db.Prescriptions.Any())
        {
            db.Prescriptions.AddRange([
                new Prescription
                {
                    MedicalRecordId = 1,
                    PatientId = 25,
                    DoctorId = 7,
                    AppointmentId = 2901,
                    PrescriptionType = PrescriptionType.Service,
                    Status = PrescriptionStatus.Finalized,
                    CreatedAt = now.AddDays(-18).AddMinutes(20),
                    UpdatedAt = now.AddDays(-18).AddMinutes(24),
                    FinalizedAt = now.AddDays(-18).AddMinutes(24),
                    Items =
                    [
                        new PrescriptionItem
                        {
                            PrescriptionId = 1,
                            MedicineId = 1,
                            MedicineName = "Paracetamol 500mg",
                            Quantity = 6,
                            Dosage = "500mg/lần",
                            Frequency = "Khi sot hoac dau, toi da 3 lan/ngay",
                            DurationDays = 3,
                            Instruction = "Uong sau an, khong dung qua lieu."
                        },
                        new PrescriptionItem
                        {
                            PrescriptionId = 1,
                            MedicineId = 3,
                            MedicineName = "Loratadine 10mg",
                            Quantity = 5,
                            Dosage = "10mg/lần",
                            Frequency = "1 lan/ngay buoi toi",
                            DurationDays = 5,
                            Instruction = "Neu buon ngu, tranh lai xe."
                        }
                    ]
                },
                new Prescription
                {
                    MedicalRecordId = 2,
                    PatientId = 28,
                    DoctorId = 9,
                    AppointmentId = 2808,
                    PrescriptionType = PrescriptionType.Insurance,
                    Status = PrescriptionStatus.Finalized,
                    CreatedAt = now.AddDays(-34).AddMinutes(32),
                    UpdatedAt = now.AddDays(-34).AddMinutes(39),
                    FinalizedAt = now.AddDays(-34).AddMinutes(39),
                    Items =
                    [
                        new PrescriptionItem
                        {
                            PrescriptionId = 2,
                            MedicineId = 6,
                            MedicineName = "Amlodipine 5mg",
                            Quantity = 30,
                            Dosage = "5mg/lần",
                            Frequency = "1 lan/ngay buoi sang",
                            DurationDays = 30,
                            Instruction = "Do huyet ap moi sang va ghi lai."
                        },
                        new PrescriptionItem
                        {
                            PrescriptionId = 2,
                            MedicineId = 7,
                            MedicineName = "Metformin 500mg",
                            Quantity = 60,
                            Dosage = "500mg/lần",
                            Frequency = "2 lan/ngay",
                            DurationDays = 30,
                            Instruction = "Uong sau bua an."
                        }
                    ]
                }]);
        }

        if (!db.ReferralLetters.Any())
        {
            db.ReferralLetters.Add(new ReferralLetter
            {
                MedicalRecordId = 2,
                PatientId = 28,
                DoctorId = 9,
                TargetHospital = "Benh vien Tim mach TP.HCM",
                Reason = "Can danh gia chuyen sau neu tuc nguc tai dien hoac huyet ap khong kiem soat.",
                ClinicalSummary = "Benh nhan tang huyet ap lau nam kem dai thao duong type 2, hien chua co dau hieu cap cuu.",
                CreatedAt = now.AddDays(-34).AddMinutes(42)
            });
        }

        if (!db.OutboxMessages.Any())
        {
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventName = "prescription.created",
                PayloadJson = """
                {"EventName":"prescription.created","PrescriptionId":2,"MedicalRecordId":2,"PatientId":28,"DoctorId":9,"AppointmentId":2808,"PrescriptionType":"Insurance"}
                """,
                Processed = true,
                CreatedAt = now.AddDays(-34).AddMinutes(39),
                ProcessedAt = now.AddDays(-34).AddMinutes(40)
            });
        }

        if (!db.MedicalRecordLogs.Any())
        {
            db.MedicalRecordLogs.AddRange([
                new MedicalRecordLog
                {
                    MedicalRecordId = 1,
                    UserId = 7,
                    Action = "medical_record.completed",
                    NewValueJson = "{\"finalDiagnosis\":\"Viem hong cap, theo doi di ung thoi tiet\"}",
                    CreatedAt = now.AddDays(-18).AddMinutes(25)
                },
                new MedicalRecordLog
                {
                    MedicalRecordId = 2,
                    UserId = 9,
                    Action = "medical_record.completed",
                    NewValueJson = "{\"finalDiagnosis\":\"Tang huyet ap do bo lieu va an man\"}",
                    CreatedAt = now.AddDays(-34).AddMinutes(40)
                }]);
        }

        db.SaveChanges();
    }
}
