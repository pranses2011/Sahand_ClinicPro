using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// بیمار ناشناس (اورژانسی) — پروندهٔ موقت بدون کد ملی با کد موقت یکتای
    /// سیستمی (UNK-14050518-003) تا زمان «تکمیل هویت» از روی پرونده؛ کد ملی
    /// برای ثبت عادی همچنان الزامی است (اعتبارسنجی فرمان ثبت دست نخورده).
    /// تغییرات: NationalCode می‌تواند تهی باشد (فقط برای ناشناس)، ستون‌های
    /// IsUnidentified و TemporaryCode، ایندکس یکتای کد موقت.
    /// سبک مهاجرت: SQL خامِ گاردشده (idempotent) هم‌سبک مهاجرت‌های اخیر.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261027120000_UnidentifiedPatient")]
    public partial class UnidentifiedPatient : Migration
    {
        /// <summary>اجرای مهاجرت — افزودن ستون‌ها و اصلاح ایندکس کد ملی</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Patients') AND name = N'IsUnidentified')
BEGIN
    ALTER TABLE [Patients] ADD [IsUnidentified] bit NOT NULL CONSTRAINT [DF_Patients_IsUnidentified] DEFAULT 0;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Patients') AND name = N'TemporaryCode')
BEGIN
    ALTER TABLE [Patients] ADD [TemporaryCode] nvarchar(20) NULL;
END

ALTER TABLE [Patients] ALTER COLUMN [NationalCode] nvarchar(10) NULL;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Patient_NationalCode' AND object_id = OBJECT_ID(N'Patients'))
BEGIN
    DROP INDEX [IX_Patient_NationalCode] ON [Patients];
END

CREATE UNIQUE INDEX [IX_Patient_NationalCode] ON [Patients] ([NationalCode])
    WHERE [IsDeleted] = 0 AND [NationalCode] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Patient_TemporaryCode' AND object_id = OBJECT_ID(N'Patients'))
BEGIN
    CREATE UNIQUE INDEX [IX_Patient_TemporaryCode] ON [Patients] ([TemporaryCode])
        WHERE [IsDeleted] = 0 AND [TemporaryCode] IS NOT NULL;
END
");
        }

        /// <summary>بازگشت — حذف ستون‌ها و بازگردانی ایندکس قبلی کد ملی</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Patient_TemporaryCode' AND object_id = OBJECT_ID(N'Patients'))
BEGIN
    DROP INDEX [IX_Patient_TemporaryCode] ON [Patients];
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Patient_NationalCode' AND object_id = OBJECT_ID(N'Patients'))
BEGIN
    DROP INDEX [IX_Patient_NationalCode] ON [Patients];
END

CREATE UNIQUE INDEX [IX_Patient_NationalCode] ON [Patients] ([NationalCode])
    WHERE [IsDeleted] = 0;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Patients') AND name = N'TemporaryCode')
BEGIN
    ALTER TABLE [Patients] DROP COLUMN [TemporaryCode];
END

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Patients') AND name = N'IsUnidentified')
BEGIN
    ALTER TABLE [Patients] DROP CONSTRAINT [DF_Patients_IsUnidentified];
    ALTER TABLE [Patients] DROP COLUMN [IsUnidentified];
END
");
        }
    }
}
