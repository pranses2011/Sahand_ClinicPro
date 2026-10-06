using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// DbContext اصلی سامانه — ۸۷ موجودیت در ۱۵ پوشه ماژولار
/// </summary>
/// <remarks>
/// قواعد سراسری مدل:
/// ۱. همه جداول دارای فیلدهای حسابرسی و حذف نرم هستند
/// ۲. فیلتر پرس‌وجوی سراسری IsDeleted روی همه موجودیت‌های قابل حذف نرم
/// ۳. رفتار حذف همه کلیدهای خارجی NoAction است (برای حذف نرم)
/// ۴. RowVersion به‌عنوان نشانگر همزمانی روی همه جدول‌ها
/// ۵. Sequenceهای SQL Server برای شماره‌های یکتای کسب‌وکار
/// </remarks>
public class ClinicProDbContext : DbContext
{
    /// <summary>
    /// حالت دیتابیس درون‌حافظه‌ای برای تست‌ها — مقدار پیش‌فرض SQL (سکانس) اجرا نمی‌شود؛
    /// برای ستون‌های الزامی سکانسی مولد سمت کاربر جایگزین می‌شود.
    /// تصمیم معماری گام V6+ مستند در remarks کلاس.
    /// </summary>
    public bool IsInMemory { get; init; }

    /// <summary>
    /// شعبه جاری برای فیلتر سراسری چندشعبه‌ای — گام U17.
    /// توسط BranchContextMiddleware از هدر X-Branch-Id تنظیم می‌شود.
    /// تهی یعنی بدون فیلتر شعبه (همه داده‌های بدون شعبه + عمومی)
    /// </summary>
    public long? CurrentBranchId { get; set; }

    /// <summary>مجموعه AccountingAccounts</summary>
    public DbSet<AccountingAccount> AccountingAccounts { get; set; }

    /// <summary>مجموعه ApiCallLogs</summary>
    public DbSet<ApiCallLog> ApiCallLogs { get; set; }

    /// <summary>مجموعه ApiCredentials</summary>
    public DbSet<ApiCredential> ApiCredentials { get; set; }

    /// <summary>مجموعه Appointments</summary>
    public DbSet<Appointment> Appointments { get; set; }

    /// <summary>مجموعه AttendanceCorrections — درخواست اصلاح کارت</summary>
    public DbSet<AttendanceCorrection> AttendanceCorrections { get; set; }

    /// <summary>مجموعه AttendanceDevices — دستگاه‌های حضور و غیاب</summary>
    public DbSet<AttendanceDevice> AttendanceDevices { get; set; }

    /// <summary>مجموعه AuditLogs</summary>
    public DbSet<AuditLog> AuditLogs { get; set; }

    /// <summary>مجموعه BackupHistory</summary>
    public DbSet<BackupHistory> BackupHistory { get; set; }

    /// <summary>مجموعه Branches</summary>
    public DbSet<Branch> Branches { get; set; }

    /// <summary>مجموعه CashRegisters</summary>
    public DbSet<CashRegister> CashRegisters { get; set; }

    /// <summary>مجموعه ClinicInfo</summary>
    public DbSet<ClinicInfo> ClinicInfo { get; set; }

    /// <summary>مجموعه ChronicCarePlans — طرح مراقبت بیمار مزمن (گام R2)</summary>
    public DbSet<ChronicCarePlan> ChronicCarePlans { get; set; }

    /// <summary>مجموعه FamilyHistories — سابقه پزشکی خانواده (گام R2)</summary>
    public DbSet<FamilyHistory> FamilyHistories { get; set; }

    /// <summary>مجموعه GrowthMeasurements — اندازه‌گیری رشد کودک (گام R2)</summary>
    public DbSet<GrowthMeasurement> GrowthMeasurements { get; set; }

    /// <summary>مجموعه ClinicLicense</summary>
    public DbSet<ClinicLicense> ClinicLicense { get; set; }

    /// <summary>مجموعه CostCenters</summary>
    public DbSet<CostCenter> CostCenters { get; set; }

    /// <summary>مجموعه DentalProcedures</summary>
    public DbSet<DentalProcedure> DentalProcedures { get; set; }

    /// <summary>مجموعه DentalRecords</summary>
    public DbSet<DentalRecord> DentalRecords { get; set; }

    /// <summary>مجموعه DentalTreatmentPlans</summary>
    public DbSet<DentalTreatmentPlan> DentalTreatmentPlans { get; set; }

    /// <summary>مجموعه Departments</summary>
    public DbSet<Department> Departments { get; set; }

    /// <summary>مجموعه Doctors</summary>
    public DbSet<Doctor> Doctors { get; set; }

    /// <summary>مجموعه DoctorAbsences</summary>
    public DbSet<DoctorAbsence> DoctorAbsences { get; set; }

    /// <summary>مجموعه DoctorPayments</summary>
    public DbSet<DoctorPayment> DoctorPayments { get; set; }

    /// <summary>مجموعه DoctorSchedules</summary>
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }

    /// <summary>مجموعه Documents</summary>
    public DbSet<Document> Documents { get; set; }

    /// <summary>مجموعه DocumentCategories</summary>
    public DbSet<DocumentCategory> DocumentCategories { get; set; }

    /// <summary>مجموعه DeviceRawLogs — لاگ خام ضربه‌ها</summary>
    public DbSet<DeviceRawLog> DeviceRawLogs { get; set; }

    /// <summary>مجموعه DeviceUserMappings — نگاشت کاربر دستگاه به پرسنل</summary>
    public DbSet<DeviceUserMapping> DeviceUserMappings { get; set; }

    /// <summary>مجموعه DrugInteractions</summary>
    public DbSet<DrugInteraction> DrugInteractions { get; set; }

    /// <summary>مجموعه ElectronicPrescriptions</summary>
    public DbSet<ElectronicPrescription> ElectronicPrescriptions { get; set; }

    /// <summary>مجموعه Employees</summary>
    public DbSet<Employee> Employees { get; set; }

    /// <summary>مجموعه EmployeeAttendances</summary>
    public DbSet<EmployeeAttendance> EmployeeAttendances { get; set; }

    /// <summary>مجموعه EmployeeLeaves</summary>
    public DbSet<EmployeeLeave> EmployeeLeaves { get; set; }

    /// <summary>مجموعه EmployeeSalaries</summary>
    public DbSet<EmployeeSalary> EmployeeSalaries { get; set; }

    /// <summary>مجموعه EmployeeShiftAssignments — تخصیص شیفت</summary>
    public DbSet<EmployeeShiftAssignment> EmployeeShiftAssignments { get; set; }

    /// <summary>مجموعه ErrorLogs</summary>
    public DbSet<ErrorLog> ErrorLogs { get; set; }

    /// <summary>مجموعه ExaminationRooms</summary>
    public DbSet<ExaminationRoom> ExaminationRooms { get; set; }

    /// <summary>مجموعه Expenses</summary>
    public DbSet<Expense> Expenses { get; set; }

    /// <summary>مجموعه EyeExamRecords</summary>
    public DbSet<EyeExamRecord> EyeExamRecords { get; set; }

    /// <summary>مجموعه FiscalYears</summary>
    public DbSet<FiscalYear> FiscalYears { get; set; }

    /// <summary>مجموعه Holidays — تقویم تعطیلات</summary>
    public DbSet<Holiday> Holidays { get; set; }

    /// <summary>مجموعه ICD10Codes</summary>
    public DbSet<Icd10Code> ICD10Codes { get; set; }

    /// <summary>مجموعه ImagingFiles</summary>
    public DbSet<ImagingFile> ImagingFiles { get; set; }

    /// <summary>مجموعه ImagingOrders</summary>
    public DbSet<ImagingOrder> ImagingOrders { get; set; }

    /// <summary>مجموعه InsuranceClaims</summary>
    public DbSet<InsuranceClaim> InsuranceClaims { get; set; }

    /// <summary>مجموعه InsuranceCompanies</summary>
    public DbSet<InsuranceCompany> InsuranceCompanies { get; set; }

    /// <summary>مجموعه InsuranceInquiries</summary>
    public DbSet<InsuranceInquiry> InsuranceInquiries { get; set; }

    /// <summary>مجموعه Invoices</summary>
    public DbSet<Invoice> Invoices { get; set; }

    /// <summary>مجموعه InvoiceItems</summary>
    public DbSet<InvoiceItem> InvoiceItems { get; set; }

    /// <summary>مجموعه JournalEntries</summary>
    public DbSet<JournalEntry> JournalEntries { get; set; }

    /// <summary>مجموعه JournalEntryLines</summary>
    public DbSet<JournalEntryLine> JournalEntryLines { get; set; }

    /// <summary>مجموعه LabOrders</summary>
    public DbSet<LabOrder> LabOrders { get; set; }

    /// <summary>مجموعه LabOrderItems</summary>
    public DbSet<LabOrderItem> LabOrderItems { get; set; }

    /// <summary>مجموعه LabTests</summary>
    public DbSet<LabTest> LabTests { get; set; }

    /// <summary>مجموعه LicenseCheckLog</summary>
    public DbSet<LicenseCheckLog> LicenseCheckLog { get; set; }

    /// <summary>مجموعه Medications</summary>
    public DbSet<Medication> Medications { get; set; }

    /// <summary>مجموعه Notifications</summary>
    public DbSet<Notification> Notifications { get; set; }

    /// <summary>مجموعه PasswordHistory</summary>
    public DbSet<PasswordHistory> PasswordHistory { get; set; }

    /// <summary>مجموعه Patients</summary>
    public DbSet<Patient> Patients { get; set; }

    /// <summary>مجموعه PatientHistories</summary>
    public DbSet<PatientHistory> PatientHistories { get; set; }

    /// <summary>مجموعه Payments</summary>
    public DbSet<Payment> Payments { get; set; }

    /// <summary>مجموعه PerformanceMetrics</summary>
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }

    /// <summary>مجموعه Permissions</summary>
    public DbSet<Permission> Permissions { get; set; }

    /// <summary>مجموعه Contracts</summary>
    public DbSet<PersonnelContract> Contracts { get; set; }

    /// <summary>مجموعه PharmacyInventory</summary>
    public DbSet<PharmacyInventory> PharmacyInventory { get; set; }

    /// <summary>مجموعه PharmacyTransactions</summary>
    public DbSet<PharmacyTransaction> PharmacyTransactions { get; set; }

    /// <summary>مجموعه PhoneCalls</summary>
    public DbSet<PhoneCall> PhoneCalls { get; set; }

    /// <summary>مجموعه PhysiotherapySessions</summary>
    public DbSet<PhysiotherapySession> PhysiotherapySessions { get; set; }

    /// <summary>مجموعه Positions</summary>
    public DbSet<Position> Positions { get; set; }

    /// <summary>مجموعه Prescriptions</summary>
    public DbSet<Prescription> Prescriptions { get; set; }

    /// <summary>مجموعه PrescriptionItems</summary>
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

    /// <summary>مجموعه PrescriptionTemplates</summary>
    public DbSet<PrescriptionTemplate> PrescriptionTemplates { get; set; }

    /// <summary>مجموعه PurchaseOrders</summary>
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    /// <summary>مجموعه PurchaseOrderItems</summary>
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    /// <summary>مجموعه Queues</summary>
    public DbSet<Queue> Queues { get; set; }

    /// <summary>مجموعه Receptions</summary>
    public DbSet<Reception> Receptions { get; set; }

    /// <summary>مجموعه ReferralSystem</summary>
    public DbSet<ReferralRecord> ReferralSystem { get; set; }

    /// <summary>مجموعه RefreshTokens</summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    /// <summary>مجموعه ReportDefinitions</summary>
    public DbSet<ReportDefinition> ReportDefinitions { get; set; }

    /// <summary>مجموعه ReportExecutions</summary>
    public DbSet<ReportExecution> ReportExecutions { get; set; }

    /// <summary>مجموعه ReportSchedules</summary>
    public DbSet<ReportSchedule> ReportSchedules { get; set; }

    /// <summary>مجموعه PrintTemplates — قالب چاپ سفارشی (چرخه ۱۳)</summary>
    public DbSet<PrintTemplate> PrintTemplates { get; set; }

    /// <summary>مجموعه Roles</summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>مجموعه RolePermissions</summary>
    public DbSet<RolePermission> RolePermissions { get; set; }

    /// <summary>مجموعه SepasTransactions</summary>
    public DbSet<SepasTransaction> SepasTransactions { get; set; }

    /// <summary>مجموعه Services</summary>
    public DbSet<Service> Services { get; set; }

    /// <summary>مجموعه ServiceCategories</summary>
    public DbSet<ServiceCategory> ServiceCategories { get; set; }

    /// <summary>مجموعه Shifts</summary>
    public DbSet<Shift> Shifts { get; set; }

    /// <summary>مجموعه SibSystemRecords</summary>
    public DbSet<SibSystemRecord> SibSystemRecords { get; set; }

    /// <summary>مجموعه SmsMessages</summary>
    public DbSet<SmsMessage> SmsMessages { get; set; }

    /// <summary>مجموعه SmsProviders</summary>
    public DbSet<SmsProvider> SmsProviders { get; set; }

    /// <summary>مجموعه SmsQueue</summary>
    public DbSet<SmsQueueItem> SmsQueue { get; set; }

    /// <summary>مجموعه SmsTemplates</summary>
    public DbSet<SmsTemplate> SmsTemplates { get; set; }

    /// <summary>مجموعه Specialties</summary>
    public DbSet<Specialty> Specialties { get; set; }

    /// <summary>مجموعه StockTakings</summary>
    public DbSet<StockTaking> StockTakings { get; set; }

    /// <summary>مجموعه StockTakingItems — گام P2</summary>
    public DbSet<StockTakingItem> StockTakingItems { get; set; }

    /// <summary>مجموعه Suppliers</summary>
    public DbSet<Supplier> Suppliers { get; set; }

    /// <summary>مجموعه Surveys</summary>
    public DbSet<Survey> Surveys { get; set; }

    /// <summary>مجموعه SurveyResponses</summary>
    public DbSet<SurveyResponse> SurveyResponses { get; set; }

    /// <summary>مجموعه Modules</summary>
    public DbSet<SystemModule> Modules { get; set; }

    /// <summary>مجموعه SystemSettings</summary>
    public DbSet<SystemSetting> SystemSettings { get; set; }

    /// <summary>مجموعه Tariffs</summary>
    public DbSet<Tariff> Tariffs { get; set; }

    /// <summary>مجموعه Users</summary>
    public DbSet<User> Users { get; set; }

    /// <summary>مجموعه UserModulePermissions</summary>
    public DbSet<UserModulePermission> UserModulePermissions { get; set; }

    /// <summary>مجموعه UserRoles</summary>
    public DbSet<UserRole> UserRoles { get; set; }

    /// <summary>مجموعه UserSessions</summary>
    public DbSet<UserSession> UserSessions { get; set; }

    /// <summary>مجموعه Visits</summary>
    public DbSet<Visit> Visits { get; set; }

    /// <summary>مجموعه VitalSigns</summary>
    public DbSet<VitalSign> VitalSigns { get; set; }

    public ClinicProDbContext(DbContextOptions<ClinicProDbContext> options) : base(options)
    {
        // تشخیص خودکار حالت درون‌حافظه‌ای — برای مولد مقدار MRN در تست‌ها.
        // نکته حیاتی: از options.Extensions خوانده می‌شود نه Database.ProviderName؛
        // چون دسترسی به Database مدل را همین‌جا می‌سازد و پرچم هنوز مقدار نگرفته است.
        // نام نوع extension بدون ارجاع پکیج InMemory مقایسه می‌شود (نوع داخلی است).
        IsInMemory = options.Extensions.Any(e =>
            string.Equals(e.GetType().Name, "InMemoryOptionsExtension", StringComparison.Ordinal));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // اعمال همه پیکربندی‌های موجودیت از این اسمبلی
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicProDbContext).Assembly);

        // در حالت درون‌حافظه‌ای — سکانس SQL کار نمی‌کند؛ مولد سمت کاربر برای ستون الزامی
        if (IsInMemory)
        {
            var mrn = modelBuilder.Entity<Patient>()
                .Property(p => p.MedicalRecordNo);
            mrn.HasDefaultValueSql(null);
            mrn.HasValueGenerator<MrnInMemoryValueGenerator>();
            mrn.Metadata.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
        }

        // روابط صریح موارد چند‌ناوبری — بیمار دو بیمه و پزشک دو تخصص دارد
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.InsuranceCompany)
            .WithMany(i => i.Patients)
            .HasForeignKey(p => p.InsuranceCompanyId);
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.SupplementaryInsurance)
            .WithMany()
            .HasForeignKey(p => p.SupplementaryInsuranceId);
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Specialty)
            .WithMany(s => s.Doctors)
            .HasForeignKey(d => d.SpecialtyId);
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.SubSpecialty)
            .WithMany()
            .HasForeignKey(d => d.SubSpecialtyId);

        // سکانس‌های شماره یکتا — ۱۵ سکانس کسب‌وکاری با شروع از ۱۰۰۰۰۱
        modelBuilder.HasSequence<long>("SeqPatientMrn").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqAppointmentCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqReceptionNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqVisitCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPrescriptionCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqInvoiceNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPaymentNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqJournalEntryNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqLabOrderNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqImagingOrderNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPharmacyTransactionNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqClaimNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqExpenseNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqContractNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPurchaseOrderNumber").StartsAt(100001).IncrementsBy(1);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // فیلتر سراسری حذف نرم + شعبه — گام U17:
            // رکوردهای حذف‌شده پنهان می‌شوند و رکوردهای شعبه‌دار فقط وقتی شعبه جاری
            // تنظیم شده باشد نمایش داده می‌شوند (رکوردهای بدون شعبه همیشه عمومی‌اند)
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                Expression body = Expression.Not(
                    Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));

                var branchProp = clrType.GetProperty(nameof(AuditableEntity.BranchId));
                if (branchProp is not null)
                {
                    // e => !e.IsDeleted && (e.BranchId == null || e.BranchId == ctx.CurrentBranchId)
                    var ctx = Expression.Constant(this);
                    var currentBranch = Expression.Property(ctx, nameof(CurrentBranchId));
                    body = Expression.AndAlso(body, Expression.OrElse(
                        Expression.Equal(Expression.Property(parameter, branchProp), Expression.Constant(null, branchProp.PropertyType)),
                        Expression.Equal(Expression.Property(parameter, branchProp), currentBranch)));
                }

                var filter = Expression.Lambda(body, parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }

            // رفتار حذف همه کلیدهای خارجی NoAction — الزام حذف نرم
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }

            // RowVersion به‌عنوان نشانگر همزمانی خوش‌بینانه
            var rowVersion = entityType.FindProperty(nameof(AuditableEntity.RowVersion));
            if (rowVersion is not null)
            {
                modelBuilder.Entity(clrType).Property(nameof(AuditableEntity.RowVersion)).IsRowVersion();
            }
        }
    }
}
