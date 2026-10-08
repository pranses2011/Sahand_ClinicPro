using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخه بیست‌وچهارم (گزارش هفدهم مالک) — «پذیرش خدمت درمانی»:
    /// «برای خدمات تزریقات و پانسمان هیچ قابلیتی در برنامه نیست؛ بیماری که برای
    /// آمپول/سرم/پانسمان یا فقط آزمایش/عکس می‌آید چه شود؟ قسمت پذیرش فقط پزشک
    /// دارد» — پذیرش به دو گونه توسعه یافت: ویزیت پزشک (DoctorId) یا خدمت
    /// درمانی (ServiceId از تعرفه‌ها + پرسنل انجام‌دهنده).
    ///
    /// تغییرات ساختاری این مهاجرت (همه idempotent — با گارد شرطی):
    ///   ۱) اختیاری‌کردن ستون DoctorId جدول Receptions (حذف FK الزامی →
    ///      ALTER به NULL مجاز → بازافزودن FK اختیاری) — ردیف‌های پذیرش خدمتی
    ///      بدون پزشک ذخیره می‌شوند؛ داده‌های موجود دست‌نخورده می‌مانند.
    ///   ۲) افزودن ستون‌های ServiceId (کلید خارجی اختیاری به Services)،
    ///      ServiceName (نام نمایشی پایدار) و PerformedByEmployeeId (پرسنل
    ///      انجام‌دهنده — کلید خارجی اختیاری به Employees).
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261009180000_Cycle24ReceptionServices")]
    public partial class Cycle24ReceptionServices : Migration
    {
        /// <summary>
        /// اجرای مهاجرت — هر گام با EXISTS گارد شده تا روی هر نصبی (با هر
        /// نام قید قبلی یا بدون آن) بی‌خطا اجرا شود.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── گام ۱: حذف قید کلید خارجی الزامی پزشک (در صورت وجود) ───
            // نام قید استاندارد EF در مهاجرت InitialCreate است؛ جست‌وجو با
            // الگوی عمومی «FK_Receptions_Doctors%» پوشش همهٔ حالت‌ها را می‌دهد
            migrationBuilder.Sql(@"
DECLARE @fkName nvarchar(256);
SELECT TOP 1 @fkName = fk.name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'Receptions')
  AND fk.name LIKE N'FK_Receptions_Doctors%';
IF @fkName IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(512) = N'ALTER TABLE [Receptions] DROP CONSTRAINT [' + @fkName + N']';
    EXEC sp_executesql @sql;
END
");

            // ─── گام ۲: اختیاری‌کردن ستون DoctorId ───
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'DoctorId'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE [Receptions] ALTER COLUMN [DoctorId] bigint NULL;
END
");

            // ─── گام ۳: بازافزودن FK اختیاری پزشک ───
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Receptions_Doctors_DoctorId'
      AND parent_object_id = OBJECT_ID(N'Receptions'))
BEGIN
    ALTER TABLE [Receptions]
        ADD CONSTRAINT [FK_Receptions_Doctors_DoctorId]
        FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]);
END
");

            // ─── گام ۴: ستون‌های جدید خدمت درمانی (اگر نباشند) ───
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'ServiceId')
BEGIN
    ALTER TABLE [Receptions] ADD [ServiceId] bigint NULL;

    ALTER TABLE [Receptions]
        ADD CONSTRAINT [FK_Receptions_Services_ServiceId]
        FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'ServiceName')
BEGIN
    ALTER TABLE [Receptions] ADD [ServiceName] nvarchar(200) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'PerformedByEmployeeId')
BEGIN
    ALTER TABLE [Receptions] ADD [PerformedByEmployeeId] bigint NULL;

    ALTER TABLE [Receptions]
        ADD CONSTRAINT [FK_Receptions_Employees_PerformedByEmployeeId]
        FOREIGN KEY ([PerformedByEmployeeId]) REFERENCES [Employees] ([Id]);
END
");
        }

        /// <summary>
        /// بازگشت — ستون‌های خدمت حذف و پزشک الزامی می‌شود؛ اگر پذیرش خدمتی
        /// بدون پزشک ثبت شده باشد، حذف ستون ممکن است با نقض قید شکست بخورد؛
        /// بنابراین ابتدا آن ردیف‌ها با پزشک صفر نشانه‌گذاری می‌شوند.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE r SET r.DoctorId = 0
FROM [Receptions] r
WHERE r.DoctorId IS NULL;

DECLARE @fkName nvarchar(256);
SELECT TOP 1 @fkName = fk.name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'Receptions')
  AND (fk.name LIKE N'FK_Receptions_Services%' OR fk.name LIKE N'FK_Receptions_Employees%');
WHILE @fkName IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(512) = N'ALTER TABLE [Receptions] DROP CONSTRAINT [' + @fkName + N']';
    EXEC sp_executesql @sql;
    SET @fkName = NULL;
    SELECT TOP 1 @fkName = fk.name
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'Receptions')
      AND (fk.name LIKE N'FK_Receptions_Services%' OR fk.name LIKE N'FK_Receptions_Employees%');
END

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'ServiceId')
    ALTER TABLE [Receptions] DROP COLUMN [ServiceId];
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'ServiceName')
    ALTER TABLE [Receptions] DROP COLUMN [ServiceName];
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'PerformedByEmployeeId')
    ALTER TABLE [Receptions] DROP COLUMN [PerformedByEmployeeId];
");
        }
    }
}
