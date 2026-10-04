using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "SeqAppointmentCode",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqClaimNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqContractNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqExpenseNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqImagingOrderNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqInvoiceNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqJournalEntryNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqLabOrderNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqPatientMrn",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqPaymentNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqPharmacyTransactionNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqPrescriptionCode",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqPurchaseOrderNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqReceptionNumber",
                startValue: 100001L);

            migrationBuilder.CreateSequence(
                name: "SeqVisitCode",
                startValue: 100001L);

            migrationBuilder.CreateTable(
                name: "AccountingAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "کد حساب - یکتا"),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام حساب"),
                    AccountType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع حساب - دارایی یا بدهی یا درآمد"),
                    ParentAccountId = table.Column<long>(type: "bigint", nullable: true),
                    Level = table.Column<byte>(type: "tinyint", nullable: false, comment: "سطح حساب"),
                    IsDetail = table.Column<bool>(type: "bit", nullable: false),
                    Nature = table.Column<byte>(type: "tinyint", nullable: false, comment: "طبیعت حساب"),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "تراز افتتاحیه"),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "تراز جاری"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingAccounts_AccountingAccounts_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AccountingAccounts",
                        principalColumn: "Id");
                },
                comment: "سرفصل حساب - چهارسطحی: گروه و کل و معین و تفصیلی");

            migrationBuilder.CreateTable(
                name: "ApiCallLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام سرویس مثل سپاس"),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نشانی فراخوانی‌شده"),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "فعل HTTP"),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    CalledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiCallLogs", x => x.Id);
                },
                comment: "لاگ فراخوانی API - تمام تماس‌های خارجی با پایش زمان");

            migrationBuilder.CreateTable(
                name: "ApiCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام سرویس - یکتا"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام کاربری"),
                    PasswordEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ApiKeyEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "توکن دسترسی"),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نشانی پایه سرویس"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiCredentials", x => x.Id);
                },
                comment: "اعتبارسنجی سرویس خارجی - رمزنگاری‌شده با AES-256");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام کاربری در لحظه عملیات"),
                    ActionType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع عملیات"),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام موجودیت تغییر‌یافته"),
                    EntityId = table.Column<long>(type: "bigint", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نشانی IP"),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "ماژول عملیات"),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false, comment: "شدت رخداد"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                },
                comment: "لاگ عملیات - ثبت تمام تغییرات داده‌ها با مقادیر قبل و بعد");

            migrationBuilder.CreateTable(
                name: "BackupHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackupType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع پشتیبان"),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر فایل پشتیبان"),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    StorageType = table.Column<byte>(type: "tinyint", nullable: false, comment: "محل ذخیره"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistory", x => x.Id);
                },
                comment: "تاریخچه پشتیبان‌گیری - سه‌سطحی: کامل و افزایشی و لاگ");

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام شعبه"),
                    BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای شعبه"),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "آدرس"),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شهر"),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "استان"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن"),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                },
                comment: "شعبه درمانگاه - زیرساخت چندشعبه‌ای برای آینده");

            migrationBuilder.CreateTable(
                name: "CashRegisters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegisterName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام صندوق"),
                    CashierId = table.Column<long>(type: "bigint", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "موجودی اولیه"),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "موجودی پایانی"),
                    TotalCash = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "جمع نقدی"),
                    TotalCard = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "جمع کارتی"),
                    TotalOnline = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "جمع آنلاین"),
                    Difference = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "اختلاف شمارش"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت صندوق"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisters", x => x.Id);
                },
                comment: "صندوق - کارکرد روزانه صندوقدار با موجودی");

            migrationBuilder.CreateTable(
                name: "ClinicInfo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام درمانگاه"),
                    ClinicNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام انگلیسی"),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر لوگو"),
                    LicenseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "شماره مجوز"),
                    EconomicCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد اقتصادی"),
                    NationalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "شناسه ملی"),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "نشانی"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن"),
                    Fax = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "فکس"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "ایمیل"),
                    Website = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "وب‌سایت"),
                    ManagerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "نام مدیر"),
                    FoundationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInfo", x => x.Id);
                },
                comment: "اطلاعات درمانگاه - مشخصات هویتی برای سربرگ چاپ");

            migrationBuilder.CreateTable(
                name: "ClinicLicense",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicenseKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "کلید لایسنس"),
                    ActivationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد فعال‌سازی"),
                    LicenseType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع لایسنس - اشتراکی یا دائمی"),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxDoctors = table.Column<int>(type: "int", nullable: false),
                    MaxPatients = table.Column<int>(type: "int", nullable: false),
                    MaxBranches = table.Column<int>(type: "int", nullable: false),
                    EnabledModules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HardwareFingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "اثر انگشت سخت‌افزاری - SHA-256 از شش مؤلفه"),
                    SignatureData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LastValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsOfflineActivation = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicLicense", x => x.Id);
                },
                comment: "لایسنس محلی درمانگاه - رکورد رمزنگاری‌شده با امضای RSA-2048");

            migrationBuilder.CreateTable(
                name: "DentalProcedures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام عمل"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتا"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته"),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت پیش‌فرض"),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DentalProcedures", x => x.Id);
                },
                comment: "عمل دندانپزشکی - کاتالوگ اعمال دندانپزشکی");

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام بخش"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای بخش"),
                    ManagerEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    ParentDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Floor = table.Column<int>(type: "int", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن بخش"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                },
                comment: "بخش سازمانی درمانگاه - ساختار درختی");

            migrationBuilder.CreateTable(
                name: "DocumentCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام دسته"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتا"),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    IconPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "آیکون"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentCategories_DocumentCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "DocumentCategories",
                        principalColumn: "Id");
                },
                comment: "دسته سند - طبقه‌بندی درختی فایل‌ها");

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<byte>(type: "tinyint", nullable: false, comment: "سطح خطا"),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "محل وقوع خطا"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    RequestUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نشانی درخواست"),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                },
                comment: "لاگ خطای سیستم - برای پایش سلامت نرم‌افزار");

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('EXP-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqExpenseNumber AS varchar(10)), 6))", comment: "شماره سند هزینه - یکتا"),
                    Category = table.Column<byte>(type: "tinyint", nullable: false, comment: "دسته هزینه"),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ به ریال"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Method = table.Column<byte>(type: "tinyint", nullable: false, comment: "روش پرداخت"),
                    ReceiptImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                },
                comment: "هزینه - پرداخت‌های جاری درمانگاه");

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                },
                comment: "سال مالی - دوره حسابداری");

            migrationBuilder.CreateTable(
                name: "ICD10Codes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "کد ICD-10 مثل E11 - یکتا"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "شرح انگلیسی تشخیص"),
                    DescriptionFa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "شرح فارسی تشخیص"),
                    ParentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد والد در طبقه‌بندی"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته‌بندی"),
                    Chapter = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "فصل ICD-10"),
                    Block = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "بلوک ICD-10"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ICD10Codes", x => x.Id);
                },
                comment: "کد تشخیص ICD-10 - طبقه‌بندی بین‌المللی بیماری‌ها");

            migrationBuilder.CreateTable(
                name: "InsuranceCompanies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام شرکت بیمه"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای بیمه"),
                    Type = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع بیمه"),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر لوگو"),
                    ContractNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "شماره قرارداد"),
                    ContractStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DefaultPatientShare = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "سهم پیش‌فرض بیمار"),
                    DefaultInsuranceShare = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "سهم پیش‌فرض بیمه"),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نشانی API استعلام"),
                    ApiUsername = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام کاربری API"),
                    ApiPasswordEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ApiToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "توکن پایدار API"),
                    SupportsOnlineInquiry = table.Column<bool>(type: "bit", nullable: false),
                    SupportsElectronicRx = table.Column<bool>(type: "bit", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن رابط"),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "نشانی"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceCompanies", x => x.Id);
                },
                comment: "شرکت بیمه - اطلاعات قرارداد و اتصال API");

            migrationBuilder.CreateTable(
                name: "LabTests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد تست - یکتا"),
                    TestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام آزمایش"),
                    TestNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام انگلیسی"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته آزمایش مثل بیوشیمی"),
                    SampleType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نوع نمونه مثل خون یا ادرار"),
                    ContainerType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نوع ظرف نمونه"),
                    NormalRangeMale = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "حد طبیعی آقایان"),
                    NormalRangeFemale = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "حد طبیعی خانم‌ها"),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "واحد سنجش"),
                    Price = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت"),
                    TurnAroundTime = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTests", x => x.Id);
                },
                comment: "آزمایش - کاتالوگ تست‌های آزمایشگاهی");

            migrationBuilder.CreateTable(
                name: "LicenseCheckLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Result = table.Column<byte>(type: "tinyint", nullable: false, comment: "نتیجه"),
                    IsOnlineCheck = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseCheckLog", x => x.Id);
                },
                comment: "لاگ اعتبارسنجی لایسنس - هر بررسی روزانه ثبت می‌شود");

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IrcCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد IRC دارو - یکتا"),
                    GenericName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام ژنریک انگلیسی"),
                    GenericNameFa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام ژنریک فارسی"),
                    BrandName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام تجاری"),
                    AtcCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد ATC طبقه‌بندی دارویی"),
                    Form = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "فرم دارویی مثل قرص یا شربت"),
                    Strength = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "غلظت مثل 500 میلی‌گرم"),
                    Manufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "شرکت سازنده"),
                    Country = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "کشور سازنده"),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "بارکد"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته دارویی"),
                    IsOtc = table.Column<bool>(type: "bit", nullable: false),
                    IsNarcotic = table.Column<bool>(type: "bit", nullable: false),
                    IsRefrigerated = table.Column<bool>(type: "bit", nullable: false),
                    SideEffects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contraindications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                },
                comment: "دارو - کاتالوگ مرجع داروها با کد IRC");

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام ماژول"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای ماژول"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته ماژول"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "آیکون"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCore = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "نسخه ماژول"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                },
                comment: "ماژول سیستم - فهرست ماژول‌ها با وضعیت فعال‌سازی لایسنس");

            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetricType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نوع سنجه مثل CPU"),
                    MetricName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام سنجه"),
                    Value = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مقدار اندازه‌گیری"),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "واحد"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                },
                comment: "سنجه عملکرد - نمونه‌برداری دوره‌ای سلامت سیستم");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام یکتای دسترسی مثل patients.create"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته‌بندی"),
                    Module = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام ماژول"),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نوع عملیات مثل view یا create"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                },
                comment: "دسترسی - مجوز اتمی بر اساس ماژول و عملیات");

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام گزارش"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای گزارش"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته گزارش"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportFile = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SqlQuery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredPermission = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسترسی موردنیاز اجرا"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                },
                comment: "تعریف گزارش - قالب فایل گزارش یا کوئری SQL");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام نقش - یکتا"),
                    NormalizedName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام نرمال‌شده برای جستجو"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                },
                comment: "نقش - دسته‌بندی دسترسی کاربران");

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام دسته"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتا"),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCategories_ServiceCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id");
                },
                comment: "دسته‌بندی خدمات - ساختار درختی");

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام شیفت"),
                    ShiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای شیفت"),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ShiftType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع شیفت"),
                    IsNightShift = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                },
                comment: "شیفت کاری - الگوی زمانی کار پرسنل");

            migrationBuilder.CreateTable(
                name: "SmsProviders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام پنل مثل کاوه‌نگار"),
                    ProviderCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای پنل"),
                    ApiUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نشانی API"),
                    ApiKeyEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام کاربری پنل"),
                    PasswordEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SenderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "شماره فرستنده"),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "موجودی اعتبار"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsProviders", x => x.Id);
                },
                comment: "پنل پیامک - تنظیمات ده پنل ایرانی با احراز رمزنگاری‌شده");

            migrationBuilder.CreateTable(
                name: "SmsQueue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Recipient = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false, comment: "شماره گیرنده"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت در صف"),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsQueue", x => x.Id);
                },
                comment: "صف ارسال پیامک - پردازش پس‌زمینه با Hangfire");

            migrationBuilder.CreateTable(
                name: "SmsTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام قالب"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای قالب"),
                    TemplateType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع قالب"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Variables = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsTemplates", x => x.Id);
                },
                comment: "قالب پیامک - الگوی آماده با متغیرها");

            migrationBuilder.CreateTable(
                name: "Specialties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام تخصص به فارسی"),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام تخصص به انگلیسی"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای تخصص"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "مسیر آیکون"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specialties_Specialties_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Specialties",
                        principalColumn: "Id");
                },
                comment: "تخصص پزشکی - درختی با ارجاع به والد برای فوق‌تخصص‌ها");

            migrationBuilder.CreateTable(
                name: "StockTakings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TakingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PerformedBy = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakings", x => x.Id);
                },
                comment: "انبارگردانی - شمارش ادواری موجودی");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام شرکت"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتا"),
                    SupplierType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع تأمین‌کننده"),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "شخص رابط"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن"),
                    Mobile = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true, comment: "موبایل"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "ایمیل"),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "نشانی"),
                    EconomicCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد اقتصادی"),
                    NationalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "شناسه ملی شرکت"),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "حساب بانکی"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                },
                comment: "تأمین‌کننده دارو - شرکت‌های پخش و سازنده");

            migrationBuilder.CreateTable(
                name: "Surveys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان نظرسنجی"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SurveyType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع نظرسنجی"),
                    Questions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surveys", x => x.Id);
                },
                comment: "نظرسنجی - پرسشنامه رضایت بیماران");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته تنظیم مثل عمومی یا مالی"),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "کلید تنظیم - یکتا"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع داده"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                },
                comment: "تنظیمات سیستم - کلید و مقدار با پشتیبانی رمزنگاری");

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام مرکز هزینه"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتا"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostCenters_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                },
                comment: "مرکز هزینه - تخصیص هزینه‌ها به بخش‌ها");

            migrationBuilder.CreateTable(
                name: "ExaminationRooms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "شماره اتاق - یکتا"),
                    RoomName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام اتاق"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Floor = table.Column<int>(type: "int", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    RoomType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع اتاق"),
                    Equipment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExaminationRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExaminationRooms_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                },
                comment: "اتاق ویزیت - ظرفیت فیزیکی درمانگاه");

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان سمت"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد یکتای سمت"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinSalary = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حداقل حقوق"),
                    MaxSalary = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حداکثر حقوق"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                },
                comment: "سمت شغلی - عنوان‌های سازمانی با دامنه حقوق");

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان سند"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع رکورد مرجع"),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام فایل"),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر ذخیره‌سازی"),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "پسوند فایل"),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "نوع MIME"),
                    Hash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "هش فایل برای بررسی یکپارچگی"),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "برچسب‌ها"),
                    UploadedBy = table.Column<long>(type: "bigint", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "DocumentCategories",
                        principalColumn: "Id");
                },
                comment: "سند - فایل پیوست هر رکورد سیستم");

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('JE-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqJournalEntryNumber AS varchar(10)), 6))", comment: "شماره سند - یکتا"),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EntryDateShamsi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "تاریخ شمسی سند"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "منشأ سند - فاکتور یا پرداخت یا دستی"),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "جمع بدهکار"),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "جمع بستانکار"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت سند"),
                    FiscalYearId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id");
                },
                comment: "سند حسابداری دوبل - خلاصه تراکنش مالی");

            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('CLM-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqClaimNumber AS varchar(10)), 6))", comment: "شماره مطالبه - یکتا"),
                    InsuranceCompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ کل مطالبه"),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ تأییدشده"),
                    RejectedAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ ردشده"),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ دریافتی"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت مطالبه"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                },
                comment: "مطالبه بیمه - جمع ثبت‌شده برای ارسال به بیمهگر");

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalRecordNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValueSql: "CONCAT('MRN-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPatientMrn AS varchar(10)), 6))", comment: "شماره پرونده پزشکی - تولید خودکار با فرمت MRN-XXXXXX"),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "کد ملی ۱۰ رقمی - یکتا و با الگوریتم رقم کنترلی اعتبارسنجی می‌شود"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام بیمار - حداکثر ۵۰ کاراکتر"),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام خانوادگی بیمار - ایندکس‌شده برای جستجو"),
                    FatherName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام پدر بیمار"),
                    Gender = table.Column<byte>(type: "tinyint", nullable: false, comment: "جنسیت بیمار"),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BirthDateShamsi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "تاریخ تولد شمسی موازی - فرمت 1358/04/15"),
                    BloodType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "گروه خونی مثل A+ یا O-"),
                    MaritalStatus = table.Column<byte>(type: "tinyint", nullable: true, comment: "وضعیت تأهل"),
                    Mobile = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false, comment: "شماره موبایل ۱۱ رقمی - الگوی 09XXXXXXXXX - ایندکس‌شده"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن ثابت"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "ایمیل بیمار"),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "آدرس - با AES-256 رمزنگاری می‌شود"),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شهر"),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "استان"),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "کد پستی ۱۰ رقمی"),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام تماس اضطراری"),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true, comment: "تلفن تماس اضطراری"),
                    EmergencyContactRelation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نسبت تماس اضطراری با بیمار"),
                    InsuranceCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    InsuranceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "شماره دفترچه بیمه"),
                    InsuranceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نوع بیمه - اصلی یا تبعی"),
                    SupplementaryInsuranceId = table.Column<long>(type: "bigint", nullable: true),
                    SepasPatientId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شناسه بیمار در سامانه سپاس"),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "شغل بیمار"),
                    Education = table.Column<byte>(type: "tinyint", nullable: true, comment: "سطح تحصیلات"),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "ملیت - پیش‌فرض ایرانی"),
                    PatientType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع بیمار - عادی یا ویژه یا کارمند"),
                    Allergies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChronicDiseases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicture = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر عکس پروفایل"),
                    EncryptedData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastVisitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_InsuranceCompanies_SupplementaryInsuranceId",
                        column: x => x.SupplementaryInsuranceId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                },
                comment: "بیمار — مرکزی‌ترین موجودیت سیستم که تمام ماژول‌ها به آن وابسته هستند");

            migrationBuilder.CreateTable(
                name: "DrugInteractions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Medication1Id = table.Column<long>(type: "bigint", nullable: false),
                    Medication2Id = table.Column<long>(type: "bigint", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false, comment: "شدت تداخل"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mechanism = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "سازوکار تداخل"),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugInteractions_Medications_Medication1Id",
                        column: x => x.Medication1Id,
                        principalTable: "Medications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrugInteractions_Medications_Medication2Id",
                        column: x => x.Medication2Id,
                        principalTable: "Medications",
                        principalColumn: "Id");
                },
                comment: "تداخل دارویی - جفت داروهای خطرناک با شدت تداخل");

            migrationBuilder.CreateTable(
                name: "ReportExecutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    ExportFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "قالب خروجی مثل PDF"),
                    RowCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportExecutions_ReportDefinitions_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id");
                },
                comment: "اجرا گزارش - رکورد هر بار تولید گزارش");

            migrationBuilder.CreateTable(
                name: "ReportSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    Frequency = table.Column<byte>(type: "tinyint", nullable: false, comment: "دوره تکرار"),
                    ScheduleTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Recipients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextRun = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRun = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportSchedules_ReportDefinitions_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id");
                },
                comment: "زمان‌بندی گزارش - تولید خودکار دوره‌ای");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                },
                comment: "رابطه چند‌به‌چند نقش و دسترسی - کلید مرکب");

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد خدمت - یکتا"),
                    ServiceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام خدمت"),
                    ServiceNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام انگلیسی خدمت"),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع خدمت"),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "واحد سنجش"),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت پیش‌فرض"),
                    KCoefficient = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "ضریب K"),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "کد ملی خدمت برای بیمه"),
                    RequiresAuthorization = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_ServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id");
                },
                comment: "خدمت - فهرست خدمات قابل ارائه با قیمت پایه");

            migrationBuilder.CreateTable(
                name: "SmsMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Recipient = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false, comment: "شماره گیرنده - ۱۱ رقم"),
                    RecipientType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع گیرنده"),
                    RecipientId = table.Column<long>(type: "bigint", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateId = table.Column<long>(type: "bigint", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام پنل ارسال‌کننده"),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "شناسه پیام در پنل"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت ارسال"),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "هزینه ارسال"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع پیام"),
                    ReferenceType = table.Column<byte>(type: "tinyint", nullable: true, comment: "منشأ پیام"),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsMessages_SmsTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "SmsTemplates",
                        principalColumn: "Id");
                },
                comment: "پیامک ارسالی - رکورد کامل همراه وضعیت تحویل");

            migrationBuilder.CreateTable(
                name: "PharmacyInventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationId = table.Column<long>(type: "bigint", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شماره بچ"),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "موجودی فعلی"),
                    MinStock = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حداقل موجودی"),
                    MaxStock = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حداکثر موجودی"),
                    ReorderLevel = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "نقطه سفارش مجدد"),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت خرید"),
                    SellPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت فروش"),
                    Location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "محل فیزیکی در قفسه"),
                    SupplierId = table.Column<long>(type: "bigint", nullable: true),
                    LastPurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyInventory_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PharmacyInventory_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                },
                comment: "موجودی داروخانه - به تفکیک بچ و تاریخ انقضا");

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('PO-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPurchaseOrderNumber AS varchar(10)), 6))", comment: "شماره سفارش - یکتا"),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ کل"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت سفارش"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                },
                comment: "سفارش خرید دارو - از تأمین‌کننده");

            migrationBuilder.CreateTable(
                name: "SurveyResponses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    Rating = table.Column<byte>(type: "tinyint", nullable: true),
                    Answers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyResponses_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id");
                },
                comment: "پاسخ نظرسنجی - ثبت امتیاز و نظرات بیمار");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonnelCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد پرسنلی - یکتا"),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "کد ملی - یکتا"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام"),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام خانوادگی"),
                    FatherName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام پدر"),
                    Gender = table.Column<byte>(type: "tinyint", nullable: false, comment: "جنسیت"),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MaritalStatus = table.Column<byte>(type: "tinyint", nullable: true, comment: "وضعیت تأهل"),
                    Mobile = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true, comment: "موبایل"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن ثابت"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "ایمیل"),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "آدرس"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    PositionId = table.Column<long>(type: "bigint", nullable: true),
                    EmploymentType = table.Column<byte>(type: "tinyint", nullable: true, comment: "نوع استخدام"),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractType = table.Column<byte>(type: "tinyint", nullable: true, comment: "نوع قرارداد"),
                    ContractStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حقوق پایه به ریال"),
                    BankName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام بانک"),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شماره حساب بانکی"),
                    Sheba = table.Column<string>(type: "nvarchar(26)", maxLength: 26, nullable: true, comment: "شماره شبا ۲۶ کاراکتری"),
                    EducationLevel = table.Column<byte>(type: "tinyint", nullable: true, comment: "سطح تحصیلات"),
                    FieldOfStudy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "رشته تحصیلی"),
                    EmergencyContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام تماس اضطراری"),
                    EmergencyPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "تلفن اضطراری"),
                    ProfilePicture = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر عکس"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id");
                },
                comment: "کارمند درمانگاه - پرسنل اداری و درمانی");

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryId = table.Column<long>(type: "bigint", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ بدهکار"),
                    Credit = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ بستانکار"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostCenterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_AccountingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AccountingAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id");
                },
                comment: "سطر سند حسابداری - یک حساب با بدهکار یا بستانکار");

            migrationBuilder.CreateTable(
                name: "DentalTreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DentistId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "هزینه کل"),
                    EstimatedSessions = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت طرح"),
                    PlanDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DentalTreatmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DentalTreatmentPlans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "طرح درمان دندانپزشکی - برنامه کامل مراحل درمان");

            migrationBuilder.CreateTable(
                name: "InsuranceInquiries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    InsuranceCompanyId = table.Column<long>(type: "bigint", nullable: false),
                    InquiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "کد تأیید دریافتی"),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceInquiries_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InsuranceInquiries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "استعلام بیمه - پاسخ آنلاین بیمه در لحظه پذیرش");

            migrationBuilder.CreateTable(
                name: "PatientHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    HistoryType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع سابقه"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "عنوان سابقه"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientHistories_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "سابقه پزشکی بیمار - جراحی‌ها و بستری‌ها و آلرژی‌های ثبت‌شده");

            migrationBuilder.CreateTable(
                name: "PhoneCalls",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallerNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "شماره تماس‌گیرنده"),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Direction = table.Column<byte>(type: "tinyint", nullable: false, comment: "جهت تماس"),
                    CallStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Result = table.Column<byte>(type: "tinyint", nullable: false, comment: "نتیجه تماس"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordingPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر فایل صوت"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneCalls_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "تماس تلفنی - ثبت تماس‌های مرتبط با بیماران");

            migrationBuilder.CreateTable(
                name: "PhysiotherapySessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreatmentPlanId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    TherapistId = table.Column<long>(type: "bigint", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionNumber = table.Column<int>(type: "int", nullable: false),
                    Treatments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    PainScore = table.Column<byte>(type: "tinyint", nullable: true),
                    ProgressNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت جلسه"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysiotherapySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysiotherapySessions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "جلسه فیزیوتراپی - تکرارهای دوره درمان");

            migrationBuilder.CreateTable(
                name: "ReferralSystem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    ReferringDoctorId = table.Column<long>(type: "bigint", nullable: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "کد ارجاع - یکتا"),
                    ReferralType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع ارجاع"),
                    DestinationType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نوع مقصد ارجاع"),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دلیل ارجاع"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت ارجاع"),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralSystem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralSystem_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "ارجاع - داخلی یا بین‌مرکزی با کد ارجاع");

            migrationBuilder.CreateTable(
                name: "SepasTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع تراکنش"),
                    SepasReferenceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شناسه ارجاع سپاس"),
                    RequestData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت همگام‌سازی"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SepasTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SepasTransactions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "تراکنش سپاس - ثبت ویزیت در سامانه پوشش سلامت همگا");

            migrationBuilder.CreateTable(
                name: "SibSystemRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    SibRecordId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شناسه رکورد در سیب"),
                    ServiceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع خدمت"),
                    ReferralType = table.Column<byte>(type: "tinyint", nullable: true, comment: "نوع ارجاع"),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SyncStatus = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت همگام‌سازی"),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SibSystemRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SibSystemRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "رکورد سیب - همگام‌سازی با سامانه سطح یک بهداشت");

            migrationBuilder.CreateTable(
                name: "Tariffs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    InsuranceCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت تعرفه‌ای"),
                    InsuranceShare = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد سهم بیمه"),
                    PatientShare = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد سهم بیمار"),
                    InsuranceCeiling = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "سقف بیمه"),
                    IsK = table.Column<bool>(type: "bit", nullable: false),
                    KValue = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "مقدار K"),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tariffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tariffs_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tariffs_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id");
                },
                comment: "تعرفه - قیمت سالانه خدمات برای هر بیمه یا آزاد");

            migrationBuilder.CreateTable(
                name: "PharmacyTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('PHT-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPharmacyTransactionNumber AS varchar(10)), 6))", comment: "شماره تراکنش - یکتا"),
                    InventoryId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع تراکنش"),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "تعداد"),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت واحد"),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت کل"),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "منشأ تراکنش"),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    PrescriptionId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyTransactions_PharmacyInventory_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "PharmacyInventory",
                        principalColumn: "Id");
                },
                comment: "تراکنش داروخانه - ورود و خروج و تحویل دارو");

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    MedicationId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "تعداد سفارش"),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت واحد"),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت کل"),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "تعداد دریافتی"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                },
                comment: "قلم سفارش خرید - یک دارو با مقدار");

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('CTR-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqContractNumber AS varchar(10)), 6))", comment: "شماره قرارداد - یکتا"),
                    ContractType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع قرارداد"),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Salary = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حقوق قراردادی"),
                    Terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر فایل قرارداد"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                },
                comment: "قرارداد پرسنلی - سند رسمی همکاری");

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    MedicalLicenseNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "شماره پروانه پزشکی - یکتا"),
                    NezamPezeshkiCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد نظام پزشکی - یکتا"),
                    SpecialtyId = table.Column<long>(type: "bigint", nullable: true),
                    SubSpecialtyId = table.Column<long>(type: "bigint", nullable: true),
                    AcademicRank = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "مرتبه علمی - استاد یا دانشیار و غیره"),
                    Biography = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsultationFee = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حق ویزیت به ریال"),
                    VisitDuration = table.Column<int>(type: "int", nullable: false),
                    MaxPatientsPerShift = table.Column<int>(type: "int", nullable: true),
                    DoctorPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد سهم پزشک از ویزیت"),
                    SignatureImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    StampImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    OnlineBookingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AcceptsNewPatients = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, comment: "امتیاز پزشک از ۵"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Doctors_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Doctors_Specialties_SubSpecialtyId",
                        column: x => x.SubSpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id");
                },
                comment: "پزشک - اطلاعات حرفه‌ای و قراردادی پزشکان");

            migrationBuilder.CreateTable(
                name: "EmployeeAttendances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckIn = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOut = table.Column<TimeOnly>(type: "time", nullable: true),
                    ShiftId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت حضور"),
                    WorkedHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "ساعات کارکرد"),
                    OvertimeHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "ساعات اضافه‌کاری"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAttendances_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id");
                },
                comment: "حضور و غیاب پرسنل - ورود و خروج روزانه");

            migrationBuilder.CreateTable(
                name: "EmployeeLeaves",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    LeaveType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع مرخصی"),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Days = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false, comment: "تعداد روز"),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دلیل"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaves_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                },
                comment: "مرخصی پرسنل - درخواست و تأیید");

            migrationBuilder.CreateTable(
                name: "EmployeeSalaries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    PeriodMonth = table.Column<int>(type: "int", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "حقوق پایه"),
                    Overtime = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "اضافه‌کاری"),
                    Bonus = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "پاداش"),
                    Deductions = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "کسورات"),
                    Insurance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "بیمه"),
                    Tax = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مالیات"),
                    NetAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "خالص پرداختی"),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                },
                comment: "فیش حقوقی - محاسبه ماهانه حقوق پرسنل");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام کاربری - یکتا"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "ایمیل - یکتا"),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "هش رمز عبور با BCrypt - ضریب ۱۲"),
                    SecurityStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "مهر امنیتی برای ابطال توکن‌ها"),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "کلید دومرحله‌ای"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "موبایل کاربر"),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PhoneConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "IP آخرین ورود"),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                },
                comment: "کاربر سیستم - حساب ورود پرسنل به نرم‌افزار");

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('APT-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqAppointmentCode AS varchar(10)), 6))", comment: "کد پیگیری - فرمت APT-XXXXXXXX با SQL Sequence"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    SpecialtyId = table.Column<long>(type: "bigint", nullable: true),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppointmentDateShamsi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "تاریخ شمسی نوبت"),
                    AppointmentTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت نوبت"),
                    AppointmentType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع نوبت"),
                    RoomId = table.Column<long>(type: "bigint", nullable: true),
                    QueueNumber = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<byte>(type: "tinyint", nullable: false, comment: "منبع ثبت نوبت"),
                    IsFirstVisit = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false, comment: "اولویت نوبت"),
                    EstimatedFee = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "تخمین هزینه به ریال"),
                    CancellationReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دلیل لغو نوبت"),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<long>(type: "bigint", nullable: true),
                    ReminderSent = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_ExaminationRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "ExaminationRooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id");
                },
                comment: "نوبت - قلب ماژول نوبت‌دهی");

            migrationBuilder.CreateTable(
                name: "DoctorAbsences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    AbsenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FromTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ToTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    AbsenceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع غیبت"),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دلیل غیبت"),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAbsences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorAbsences_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                },
                comment: "غیبت برنامه‌ریزی‌شده پزشک - مسدودکننده اسلات‌های نوبت");

            migrationBuilder.CreateTable(
                name: "DoctorPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalVisits = table.Column<int>(type: "int", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ ناخالص"),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "کسورات"),
                    NetAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ خالص قابل پرداخت"),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت کارانه"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorPayments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                },
                comment: "کارانه پزشک - تسویه دوره‌ای سهم پزشک از ویزیت‌ها");

            migrationBuilder.CreateTable(
                name: "DoctorSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    BreakStart = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakEnd = table.Column<TimeOnly>(type: "time", nullable: true),
                    RoomId = table.Column<long>(type: "bigint", nullable: true),
                    MaxPatients = table.Column<int>(type: "int", nullable: true),
                    SlotDuration = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_ExaminationRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "ExaminationRooms",
                        principalColumn: "Id");
                },
                comment: "برنامه کاری هفتگی پزشک - مبنای تولید اسلات‌های نوبت");

            migrationBuilder.CreateTable(
                name: "PrescriptionTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "نام قالب"),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "دسته قالب"),
                    Items = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "اقلام قالب - JSON"),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionTemplates_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                },
                comment: "قالب نسخه - نسخه‌های آماده پزشکان برای بیماری‌های پرتکرار");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان اعلان"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع اعلان"),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false, comment: "اولویت"),
                    Link = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "پیوند مقصد در صورت کلیک"),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                },
                comment: "اعلان درون‌برنامه‌ای - اعلان‌های سیستم و کسب‌وکار");

            migrationBuilder.CreateTable(
                name: "PasswordHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "هش رمز پیشین"),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                },
                comment: "تاریخچه رمز عبور - جلوگیری از تکرار رمزهای قبلی");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "مقدار توکن - یکتا و هش‌شده"),
                    JwtId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "شناسه توکن JWT مرتبط"),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "اطلاعات دستگاه"),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نشانی IP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                },
                comment: "توکن تازه‌سازی - چرخه عمر توکن بدون ورود مجدد");

            migrationBuilder.CreateTable(
                name: "UserModulePermissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ModuleId = table.Column<long>(type: "bigint", nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CanAdd = table.Column<bool>(type: "bit", nullable: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false),
                    CanPrint = table.Column<bool>(type: "bit", nullable: false),
                    CanExport = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserModulePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserModulePermissions_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserModulePermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                },
                comment: "دسترسی ماژولی کاربر - سطح دسترسی دقیق‌تر از نقش");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                },
                comment: "رابطه چند‌به‌چند کاربر و نقش - کلید مرکب");

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "توکن نشست"),
                    LoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LogoutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نشانی IP"),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع دستگاه"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                },
                comment: "نشست کاربر - ردیابی دستگاه‌های متصل");

            migrationBuilder.CreateTable(
                name: "Receptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceptionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('RCP-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqReceptionNumber AS varchar(10)), 6))", comment: "شماره پذیرش - فرمت RCP-XXXXXXXX"),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    ReceptionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceptionDateShamsi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "تاریخ شمسی پذیرش"),
                    InsuranceCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    InsuranceVerified = table.Column<bool>(type: "bit", nullable: false),
                    InsuranceApprovalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "کد تأیید بیمه"),
                    SupplementaryInsuranceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد بیمه تکمیلی"),
                    ReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "کد ارجاع"),
                    ReferringDoctor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "نام پزشک ارجاع‌دهنده"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت پذیرش"),
                    ReceptionistId = table.Column<long>(type: "bigint", nullable: true),
                    EnterTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WaitingTime = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receptions_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Receptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Receptions_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Receptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "پذیرش - ورود بیمار به جریان ویزیت در روز مراجعه");

            migrationBuilder.CreateTable(
                name: "Queues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceptionId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    QueueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QueueNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت در صف"),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false, comment: "اولویت"),
                    ArrivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Queues_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Queues_Receptions_ReceptionId",
                        column: x => x.ReceptionId,
                        principalTable: "Receptions",
                        principalColumn: "Id");
                },
                comment: "صف انتظار پویا - فراخوان بیماران نزد پزشک");

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('VST-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqVisitCode AS varchar(10)), 6))", comment: "کد ویزیت - فرمت VST-XXXXXXXX"),
                    ReceptionId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VisitDateShamsi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "تاریخ شمسی ویزیت"),
                    VisitType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع ویزیت"),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfPresentIllness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PastMedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewOfSystems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhysicalExamination = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoapSubjective = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoapObjective = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoapAssessment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoapPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryDiagnosisId = table.Column<long>(type: "bigint", nullable: true),
                    SecondaryDiagnoses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreatmentPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FollowUpInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت ویزیت"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    VisitFee = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "هزینه ویزیت به ریال"),
                    SepasReferenceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد ارجاع سامانه سپاس"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Visits_ICD10Codes_PrimaryDiagnosisId",
                        column: x => x.PrimaryDiagnosisId,
                        principalTable: "ICD10Codes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Visits_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Visits_Receptions_ReceptionId",
                        column: x => x.ReceptionId,
                        principalTable: "Receptions",
                        principalColumn: "Id");
                },
                comment: "ویزیت - پرونده یک جلسه معاینه با الگوی SOAP");

            migrationBuilder.CreateTable(
                name: "DentalRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DentistId = table.Column<long>(type: "bigint", nullable: true),
                    ToothNumber = table.Column<int>(type: "int", nullable: false),
                    Surface = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "سطح دندان مثل M یا O"),
                    Condition = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت دندان"),
                    ProcedureId = table.Column<long>(type: "bigint", nullable: true),
                    ProcedureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MaterialUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "مواد مصرفی"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "هزینه"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DentalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DentalRecords_DentalProcedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "DentalProcedures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DentalRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DentalRecords_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                },
                comment: "رکورد دندانپزشکی - وضعیت هر دندان با شماره‌گذاری FDI");

            migrationBuilder.CreateTable(
                name: "EyeExamRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    RightSphere = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true, comment: "عدسی چشم راست"),
                    RightCylinder = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true, comment: "استوانه راست"),
                    RightAxis = table.Column<int>(type: "int", nullable: true),
                    LeftSphere = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true, comment: "عدسی چپ"),
                    LeftCylinder = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true, comment: "استوانه چپ"),
                    LeftAxis = table.Column<int>(type: "int", nullable: true),
                    RightVisualAcuity = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "حدت بینایی راست"),
                    LeftVisualAcuity = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "حدت بینایی چپ"),
                    RightIop = table.Column<int>(type: "int", nullable: true),
                    LeftIop = table.Column<int>(type: "int", nullable: true),
                    PupilDistance = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true, comment: "فاصقه مردمک"),
                    ColorVision = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "تست تشخیص رنگ"),
                    VisualField = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EyewearPrescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EyeExamRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EyeExamRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EyeExamRecords_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                },
                comment: "معاینه چشم - تجویز عینک و اندازه‌های چشم");

            migrationBuilder.CreateTable(
                name: "ImagingOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('IMG-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqImagingOrderNumber AS varchar(10)), 6))", comment: "شماره درخواست - یکتا"),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    ReferringDoctorId = table.Column<long>(type: "bigint", nullable: true),
                    RadiologistId = table.Column<long>(type: "bigint", nullable: true),
                    Modality = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع تصویربرداری"),
                    BodyPart = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "ناحیه بدن"),
                    ClinicalInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Technique = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Impression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false, comment: "اولویت"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagingOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ImagingOrders_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                },
                comment: "درخواست تصویربرداری - سونو یا رادیو یا CT یا MRI");

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('INV-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqInvoiceNumber AS varchar(10)), 6))", comment: "شماره فاکتور - یکتا"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    ReceptionId = table.Column<long>(type: "bigint", nullable: true),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceDateShamsi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "تاریخ شمسی فاکتور"),
                    InvoiceType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع فاکتور"),
                    InsuranceCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    SupplementaryInsuranceId = table.Column<long>(type: "bigint", nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "جمع کل قبل از تخفیف"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ تخفیف"),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد تخفیف"),
                    InsuranceDiscount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "سهم بیمه اصلی"),
                    SupplementaryDiscount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "سهم بیمه تکمیلی"),
                    Tax = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مالیات"),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ نهایی"),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ پرداخت‌شده"),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مانده حساب"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت فاکتور"),
                    PaymentStatus = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت پرداخت"),
                    CashierId = table.Column<long>(type: "bigint", nullable: true),
                    CashRegisterId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrinted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_Receptions_ReceptionId",
                        column: x => x.ReceptionId,
                        principalTable: "Receptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                },
                comment: "فاکتور - صورت‌حساب خدمات ارائه‌شده به بیمار");

            migrationBuilder.CreateTable(
                name: "LabOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('LAB-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqLabOrderNumber AS varchar(10)), 6))", comment: "شماره درخواست - یکتا"),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false, comment: "اولویت"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت درخواست"),
                    ClinicalInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleCollectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SampleCollectedBy = table.Column<long>(type: "bigint", nullable: true),
                    ResultReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabOrders_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LabOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LabOrders_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                },
                comment: "درخواست آزمایش - از پزشک برای بیمار");

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('RX-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPrescriptionCode AS varchar(10)), 6))", comment: "کد نسخه - فرمت RX-XXXXXXXX"),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PrescriptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Type = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع نسخه"),
                    IsElectronic = table.Column<bool>(type: "bit", nullable: false),
                    ElectronicTrackingCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد رهگیری نسخه الکترونیک"),
                    InsuranceSystemType = table.Column<byte>(type: "tinyint", nullable: true, comment: "سامانه مقصد نسخه الکترونیک"),
                    InsuranceApprovalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "کد تأیید بیمه"),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    IsNarcotic = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت نسخه"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prescriptions_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                },
                comment: "نسخه پزشکی - دارویی یا پاراکلینیک یا ترکیبی");

            migrationBuilder.CreateTable(
                name: "VitalSigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true, comment: "دمای بدن به سلسیوس"),
                    BloodPressureSystolic = table.Column<int>(type: "int", nullable: true),
                    BloodPressureDiastolic = table.Column<int>(type: "int", nullable: true),
                    HeartRate = table.Column<int>(type: "int", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "int", nullable: true),
                    OxygenSaturation = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true, comment: "وزن به کیلوگرم"),
                    Height = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true, comment: "قد به سانتی‌متر"),
                    Bmi = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true, comment: "شاخص توده بدنی - محاسباتی"),
                    BloodSugar = table.Column<int>(type: "int", nullable: true),
                    HeadCircumference = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true, comment: "دور سر به سانتی‌متر - برای نوزاد"),
                    MeasuredBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalSigns_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VitalSigns_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                },
                comment: "علائم حیاتی - اندازه‌گیری‌های بالینی در جریان ویزیت");

            migrationBuilder.CreateTable(
                name: "ImagingFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImagingOrderId = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام فایل"),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر ذخیره‌سازی"),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "نوع فایل"),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    IsDicom = table.Column<bool>(type: "bit", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "مسیر پیش‌نمایش"),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagingFiles_ImagingOrders_ImagingOrderId",
                        column: x => x.ImagingOrderId,
                        principalTable: "ImagingOrders",
                        principalColumn: "Id");
                },
                comment: "فایل تصویربرداری - DICOM یا تصویر معمولی");

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "تعداد"),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت واحد"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "تخفیف قلم"),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد تخفیف"),
                    InsuranceShare = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "سهم بیمه"),
                    InsurancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد بیمه"),
                    SupplementaryShare = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "سهم بیمه تکمیلی"),
                    PatientShare = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "سهم بیمار"),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "قیمت کل قلم"),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    DoctorShareAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "سهم ریالی پزشک"),
                    DoctorSharePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد سهم پزشک"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id");
                },
                comment: "قلم فاکتور - یک خدمت با جزئیات سهم بیمه و پزشک");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "CONCAT('PAY-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPaymentNumber AS varchar(10)), 6))", comment: "شماره سند پرداخت - یکتا"),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ پرداختی به ریال"),
                    Method = table.Column<byte>(type: "tinyint", nullable: false, comment: "روش پرداخت"),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شماره ارجاع تراکنش"),
                    CardNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "چهار رقم آخر کارت"),
                    BankName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام بانک"),
                    TerminalId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "شناسه پایانه کارت‌خوان"),
                    TraceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "شماره پیگیری"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت پرداخت"),
                    CashierId = table.Column<long>(type: "bigint", nullable: true),
                    CashRegisterId = table.Column<long>(type: "bigint", nullable: true),
                    IsReturned = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_CashRegisters_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "CashRegisters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "پرداخت - دریافتی نقدی یا کارتی یا آنلاین از بیمار");

            migrationBuilder.CreateTable(
                name: "LabOrderItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabOrderId = table.Column<long>(type: "bigint", nullable: false),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نتیجه متنی"),
                    ResultNumeric = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "نتیجه عددی"),
                    NormalRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "حد طبیعی"),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "واحد"),
                    IsAbnormal = table.Column<bool>(type: "bit", nullable: false),
                    AbnormalFlag = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "پرچم غیرطبیعی مثل H یا L"),
                    Comment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "توضیح آزمایشگاه"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت قلم"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabOrderItems_LabOrders_LabOrderId",
                        column: x => x.LabOrderId,
                        principalTable: "LabOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LabOrderItems_LabTests_TestId",
                        column: x => x.TestId,
                        principalTable: "LabTests",
                        principalColumn: "Id");
                },
                comment: "قلم آزمایش - نتیجه یک تست خاص");

            migrationBuilder.CreateTable(
                name: "ElectronicPrescriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    SystemType = table.Column<byte>(type: "tinyint", nullable: false, comment: "سامانه مقصد - سلامت یا تأمین"),
                    TrackingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "کد رهگیری - یکتا"),
                    RegistrationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "کد ثبت در سامانه"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت"),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "کد خطای سامانه"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicPrescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectronicPrescriptions_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                },
                comment: "نسخه الکترونیک - رکورد ارسال نسخه به سامانه سلامت یا تأمین");

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    MedicationId = table.Column<long>(type: "bigint", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: true),
                    Dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "دوز مصرف مثل یک قرص"),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "تعداد مصرف در روز"),
                    Duration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "مدت مصرف مثل هفت روز"),
                    Route = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "مسیر مصرف - خوراکی یا تزریقی"),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "تعداد کل"),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "واحد شمارش مثل عدد یا آمپول"),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSos = table.Column<bool>(type: "bit", nullable: false),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredQuantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, comment: "تعداد تحویل‌شده"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                },
                comment: "قلم نسخه - یک دارو با دستور مصرف");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingAccount_AccountCode",
                table: "AccountingAccounts",
                column: "AccountCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingAccounts_ParentId",
                table: "AccountingAccounts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiCredential_ServiceName",
                table: "ApiCredentials",
                column: "ServiceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_AppointmentCode",
                table: "Appointments",
                column: "AppointmentCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_AppointmentDate_DoctorId",
                table: "Appointments",
                columns: new[] { "AppointmentDate", "DoctorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RoomId",
                table: "Appointments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_SpecialtyId",
                table: "Appointments",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Branch_BranchCode",
                table: "Branches",
                column: "BranchCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegister_RegisterName",
                table: "CashRegisters",
                column: "RegisterName");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_EmployeeId",
                table: "Contracts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelContract_ContractNumber",
                table: "Contracts",
                column: "ContractNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_DepartmentId",
                table: "CostCenters",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalRecord_PatientId_ToothNumber",
                table: "DentalRecords",
                columns: new[] { "PatientId", "ToothNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DentalRecords_ProcedureId",
                table: "DentalRecords",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalRecords_VisitId",
                table: "DentalRecords",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalTreatmentPlans_PatientId",
                table: "DentalTreatmentPlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Code",
                table: "Departments",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentId",
                table: "Departments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAbsences_DoctorId",
                table: "DoctorAbsences",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPayment_DoctorId_PeriodStart",
                table: "DoctorPayments",
                columns: new[] { "DoctorId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_MedicalLicenseNo",
                table: "Doctors",
                column: "MedicalLicenseNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_NezamPezeshkiCode",
                table: "Doctors",
                column: "NezamPezeshkiCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_EmployeeId",
                table: "Doctors",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SpecialtyId",
                table: "Doctors",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SubSpecialtyId",
                table: "Doctors",
                column: "SubSpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorId",
                table: "DoctorSchedules",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_RoomId",
                table: "DoctorSchedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategories_ParentId",
                table: "DocumentCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CategoryId",
                table: "Documents",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugInteractions_Medication1Id",
                table: "DrugInteractions",
                column: "Medication1Id");

            migrationBuilder.CreateIndex(
                name: "IX_DrugInteractions_Medication2Id",
                table: "DrugInteractions",
                column: "Medication2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicPrescription_TrackingCode",
                table: "ElectronicPrescriptions",
                column: "TrackingCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicPrescriptions_PrescriptionId",
                table: "ElectronicPrescriptions",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_EmployeeId",
                table: "EmployeeAttendances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_ShiftId",
                table: "EmployeeAttendances",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaves_EmployeeId",
                table: "EmployeeLeaves",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_NationalCode",
                table: "Employees",
                column: "NationalCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_PersonnelCode",
                table: "Employees",
                column: "PersonnelCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaries_EmployeeId",
                table: "EmployeeSalaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationRoom_RoomNumber",
                table: "ExaminationRooms",
                column: "RoomNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationRooms_DepartmentId",
                table: "ExaminationRooms",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_ExpenseNumber",
                table: "Expenses",
                column: "ExpenseNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EyeExamRecord_PatientId",
                table: "EyeExamRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_EyeExamRecords_VisitId",
                table: "EyeExamRecords",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYear_Year",
                table: "FiscalYears",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Icd10Code_Code",
                table: "ICD10Codes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagingFiles_ImagingOrderId",
                table: "ImagingFiles",
                column: "ImagingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrder_OrderNumber",
                table: "ImagingOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrder_PatientId",
                table: "ImagingOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrders_VisitId",
                table: "ImagingOrders",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaim_ClaimNumber",
                table: "InsuranceClaims",
                column: "ClaimNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_InsuranceCompanyId",
                table: "InsuranceClaims",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompany_Code",
                table: "InsuranceCompanies",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceInquiries_InsuranceCompanyId",
                table: "InsuranceInquiries",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceInquiries_PatientId",
                table: "InsuranceInquiries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ServiceId",
                table: "InvoiceItems",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_InvoiceDate",
                table: "Invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_PatientId",
                table: "Invoices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_PaymentStatus",
                table: "Invoices",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InsuranceCompanyId",
                table: "Invoices",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ReceptionId",
                table: "Invoices",
                column: "ReceptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VisitId",
                table: "Invoices",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_FiscalYearId",
                table: "JournalEntries",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_EntryNumber",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_AccountId",
                table: "JournalEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_CostCenterId",
                table: "JournalEntryLines",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_EntryId",
                table: "JournalEntryLines",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrderItems_LabOrderId",
                table: "LabOrderItems",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrderItems_TestId",
                table: "LabOrderItems",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrder_OrderNumber",
                table: "LabOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrder_PatientId",
                table: "LabOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_DoctorId",
                table: "LabOrders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_VisitId",
                table: "LabOrders",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTest_TestCode",
                table: "LabTests",
                column: "TestCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_IrcCode",
                table: "Medications",
                column: "IrcCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SystemModule_Code",
                table: "Modules",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_UserId",
                table: "PasswordHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHistories_PatientId",
                table: "PatientHistories",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_LastName",
                table: "Patients",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_MedicalRecordNo",
                table: "Patients",
                column: "MedicalRecordNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Mobile",
                table: "Patients",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_NationalCode",
                table: "Patients",
                column: "NationalCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_InsuranceCompanyId",
                table: "Patients",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SupplementaryInsuranceId",
                table: "Patients",
                column: "SupplementaryInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaymentNumber",
                table: "Payments",
                column: "PaymentNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CashRegisterId",
                table: "Payments",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PatientId",
                table: "Payments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Name",
                table: "Permissions",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventory_ExpiryDate",
                table: "PharmacyInventory",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventory_MedicationId",
                table: "PharmacyInventory",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventory_Quantity",
                table: "PharmacyInventory",
                column: "Quantity",
                filter: "([Quantity] < [ReorderLevel] AND [IsDeleted] = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventory_SupplierId",
                table: "PharmacyInventory",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyTransaction_TransactionNumber",
                table: "PharmacyTransactions",
                column: "TransactionNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyTransactions_InventoryId",
                table: "PharmacyTransactions",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCalls_PatientId",
                table: "PhoneCalls",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysiotherapySession_PatientId_SessionDate",
                table: "PhysiotherapySessions",
                columns: new[] { "PatientId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Position_Code",
                table: "Positions",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DepartmentId",
                table: "Positions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_MedicationId",
                table: "PrescriptionItems",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_PrescriptionCode",
                table: "Prescriptions",
                column: "PrescriptionCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_VisitId",
                table: "Prescriptions",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplates_DoctorId",
                table: "PrescriptionTemplates",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_MedicationId",
                table: "PurchaseOrderItems",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_OrderId",
                table: "PurchaseOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_DoctorId",
                table: "Queues",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_ReceptionId",
                table: "Queues",
                column: "ReceptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reception_PatientId",
                table: "Receptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Reception_ReceptionNumber",
                table: "Receptions",
                column: "ReceptionNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Receptions_AppointmentId",
                table: "Receptions",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Receptions_DoctorId",
                table: "Receptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Receptions_InsuranceCompanyId",
                table: "Receptions",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRecord_ReferralCode",
                table: "ReferralSystem",
                column: "ReferralCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralSystem_PatientId",
                table: "ReferralSystem",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinition_Code",
                table: "ReportDefinitions",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_ReportId",
                table: "ReportExecutions",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_ReportId",
                table: "ReportSchedules",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name",
                table: "Roles",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SepasTransaction_SepasReferenceId",
                table: "SepasTransactions",
                column: "SepasReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SepasTransactions_PatientId",
                table: "SepasTransactions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_ParentId",
                table: "ServiceCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategory_Code",
                table: "ServiceCategories",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Service_ServiceCode",
                table: "Services",
                column: "ServiceCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId",
                table: "Services",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_ShiftCode",
                table: "Shifts",
                column: "ShiftCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SibSystemRecords_PatientId",
                table: "SibSystemRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_TemplateId",
                table: "SmsMessages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsProvider_ProviderCode",
                table: "SmsProviders",
                column: "ProviderCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplate_Code",
                table: "SmsTemplates",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_ParentId",
                table: "Specialties",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialty_Code",
                table: "Specialties",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Code",
                table: "Suppliers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyId",
                table: "SurveyResponses",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Title",
                table: "Surveys",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSetting_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tariff_ServiceId_InsuranceCompanyId_Year",
                table: "Tariffs",
                columns: new[] { "ServiceId", "InsuranceCompanyId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_Tariffs_InsuranceCompanyId",
                table: "Tariffs",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserModulePermissions_ModuleId",
                table: "UserModulePermissions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserModulePermissions_UserId",
                table: "UserModulePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Visit_DoctorId_VisitDate",
                table: "Visits",
                columns: new[] { "DoctorId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Visit_PatientId_VisitDate",
                table: "Visits",
                columns: new[] { "PatientId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Visit_VisitCode",
                table: "Visits",
                column: "VisitCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PrimaryDiagnosisId",
                table: "Visits",
                column: "PrimaryDiagnosisId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ReceptionId",
                table: "Visits",
                column: "ReceptionId",
                unique: true,
                filter: "[ReceptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_PatientId",
                table: "VitalSigns",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_VisitId",
                table: "VitalSigns",
                column: "VisitId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiCallLogs");

            migrationBuilder.DropTable(
                name: "ApiCredentials");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BackupHistory");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "ClinicInfo");

            migrationBuilder.DropTable(
                name: "ClinicLicense");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "DentalRecords");

            migrationBuilder.DropTable(
                name: "DentalTreatmentPlans");

            migrationBuilder.DropTable(
                name: "DoctorAbsences");

            migrationBuilder.DropTable(
                name: "DoctorPayments");

            migrationBuilder.DropTable(
                name: "DoctorSchedules");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "DrugInteractions");

            migrationBuilder.DropTable(
                name: "ElectronicPrescriptions");

            migrationBuilder.DropTable(
                name: "EmployeeAttendances");

            migrationBuilder.DropTable(
                name: "EmployeeLeaves");

            migrationBuilder.DropTable(
                name: "EmployeeSalaries");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "EyeExamRecords");

            migrationBuilder.DropTable(
                name: "ImagingFiles");

            migrationBuilder.DropTable(
                name: "InsuranceClaims");

            migrationBuilder.DropTable(
                name: "InsuranceInquiries");

            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "LabOrderItems");

            migrationBuilder.DropTable(
                name: "LicenseCheckLog");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordHistory");

            migrationBuilder.DropTable(
                name: "PatientHistories");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");

            migrationBuilder.DropTable(
                name: "PharmacyTransactions");

            migrationBuilder.DropTable(
                name: "PhoneCalls");

            migrationBuilder.DropTable(
                name: "PhysiotherapySessions");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "PrescriptionTemplates");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "Queues");

            migrationBuilder.DropTable(
                name: "ReferralSystem");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ReportExecutions");

            migrationBuilder.DropTable(
                name: "ReportSchedules");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SepasTransactions");

            migrationBuilder.DropTable(
                name: "SibSystemRecords");

            migrationBuilder.DropTable(
                name: "SmsMessages");

            migrationBuilder.DropTable(
                name: "SmsProviders");

            migrationBuilder.DropTable(
                name: "SmsQueue");

            migrationBuilder.DropTable(
                name: "StockTakings");

            migrationBuilder.DropTable(
                name: "SurveyResponses");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Tariffs");

            migrationBuilder.DropTable(
                name: "UserModulePermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "VitalSigns");

            migrationBuilder.DropTable(
                name: "DentalProcedures");

            migrationBuilder.DropTable(
                name: "DocumentCategories");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "ImagingOrders");

            migrationBuilder.DropTable(
                name: "AccountingAccounts");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "LabOrders");

            migrationBuilder.DropTable(
                name: "LabTests");

            migrationBuilder.DropTable(
                name: "CashRegisters");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "PharmacyInventory");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "ReportDefinitions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SmsTemplates");

            migrationBuilder.DropTable(
                name: "Surveys");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "ServiceCategories");

            migrationBuilder.DropTable(
                name: "ICD10Codes");

            migrationBuilder.DropTable(
                name: "Receptions");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "ExaminationRooms");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Specialties");

            migrationBuilder.DropTable(
                name: "InsuranceCompanies");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropSequence(
                name: "SeqAppointmentCode");

            migrationBuilder.DropSequence(
                name: "SeqClaimNumber");

            migrationBuilder.DropSequence(
                name: "SeqContractNumber");

            migrationBuilder.DropSequence(
                name: "SeqExpenseNumber");

            migrationBuilder.DropSequence(
                name: "SeqImagingOrderNumber");

            migrationBuilder.DropSequence(
                name: "SeqInvoiceNumber");

            migrationBuilder.DropSequence(
                name: "SeqJournalEntryNumber");

            migrationBuilder.DropSequence(
                name: "SeqLabOrderNumber");

            migrationBuilder.DropSequence(
                name: "SeqPatientMrn");

            migrationBuilder.DropSequence(
                name: "SeqPaymentNumber");

            migrationBuilder.DropSequence(
                name: "SeqPharmacyTransactionNumber");

            migrationBuilder.DropSequence(
                name: "SeqPrescriptionCode");

            migrationBuilder.DropSequence(
                name: "SeqPurchaseOrderNumber");

            migrationBuilder.DropSequence(
                name: "SeqReceptionNumber");

            migrationBuilder.DropSequence(
                name: "SeqVisitCode");
        }
    }
}
