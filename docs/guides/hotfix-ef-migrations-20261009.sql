-- ═══════════════════════════════════════════════════════════════════════
-- هات‌فیکس چرخه ۲۵ — رفع خطای «Invalid column name 'ServiceId'/'ServiceName'/
-- 'PerformedByEmployeeId'» سرور API
-- تاریخ: 1405/07/17 (2026-10-09)
--
-- ریشهٔ مشکل: مهاجرت‌های دست‌نویس Cycle21/Cycle23/Cycle24 فاقد ویژگی
-- [DbContext] بودند؛ به همین دلیل EF Core آن‌ها را «کشف» نمی‌کرد و
-- MigrateAsync در راه‌اندازی سرور بی‌اثر اجرا می‌شد. رفع ریشه در کد انجام
-- شده است و پس از pull و ری‌استارت سرور، مهاجرت‌ها خودکار اعمال می‌شوند.
--
-- ⚠️ این اسکریپت فقط یک مسیر جایگزینِ دستی است — اگر سرور اصلاح‌شده را
-- ری‌استارت کنید، نیازی به اجرای این فایل ندارید. همهٔ دستورها با گارد
-- شرطی نوشته شده‌اند و روی هر وضعیتی (ستون موجود/ناموجود) بی‌خطا اجرا
-- می‌شوند و در نهایت __EFMigrationsHistory دست‌نخورده می‌ماند تا سرورِ
-- اصلاح‌شده خودش ردیف‌هایش را درست ثبت کند.
-- ═══════════════════════════════════════════════════════════════════════
SET NOCOUNT ON;
PRINT N'شروع هات‌فیکس اسکیمای دیتابیس…';

-- ─── ۱) اختیاری‌کردن DoctorId جدول Receptions (پذیرش خدمت درمانی بدون پزشک) ───
DECLARE @fkName nvarchar(256);
SELECT TOP 1 @fkName = fk.name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'Receptions')
  AND fk.name LIKE N'FK_Receptions_Doctors%';
IF @fkName IS NOT NULL
BEGIN
    DECLARE @sql1 nvarchar(512) = N'ALTER TABLE [Receptions] DROP CONSTRAINT [' + @fkName + N']';
    EXEC sp_executesql @sql1;
    PRINT N'• قید کلید خارجی الزامی پزشک حذف شد: ' + @fkName;
END

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'DoctorId' AND is_nullable = 0)
BEGIN
    ALTER TABLE [Receptions] ALTER COLUMN [DoctorId] bigint NULL;
    PRINT N'• ستون DoctorId جدول Receptions اختیاری شد';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Receptions_Doctors_DoctorId'
      AND parent_object_id = OBJECT_ID(N'Receptions'))
BEGIN
    ALTER TABLE [Receptions]
        ADD CONSTRAINT [FK_Receptions_Doctors_DoctorId]
        FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]);
    PRINT N'• قید کلید خارجی اختیاری پزشک بازافزوده شد';
END

-- ─── ۲) ستون‌های خدمت درمانی جدول Receptions ───
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'ServiceId')
BEGIN
    ALTER TABLE [Receptions] ADD [ServiceId] bigint NULL;
    ALTER TABLE [Receptions]
        ADD CONSTRAINT [FK_Receptions_Services_ServiceId]
        FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]);
    PRINT N'• ستون ServiceId (شناسهٔ خدمت تعرفه‌دار) اضافه شد';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'ServiceName')
BEGIN
    ALTER TABLE [Receptions] ADD [ServiceName] nvarchar(200) NULL;
    PRINT N'• ستون ServiceName (نام خدمت) اضافه شد';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'PerformedByEmployeeId')
BEGIN
    ALTER TABLE [Receptions] ADD [PerformedByEmployeeId] bigint NULL;
    ALTER TABLE [Receptions]
        ADD CONSTRAINT [FK_Receptions_Employees_PerformedByEmployeeId]
        FOREIGN KEY ([PerformedByEmployeeId]) REFERENCES [Employees] ([Id]);
    PRINT N'• ستون PerformedByEmployeeId (پرسنل انجام‌دهنده) اضافه شد';
END

-- ─── ۳) پادآرینی چرخه ۲۳ — اختیاری‌کردن DoctorId جدول PrescriptionTemplates ───
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_PrescriptionTemplates_Doctors_DoctorId'
      AND parent_object_id = OBJECT_ID(N'PrescriptionTemplates'))
BEGIN
    ALTER TABLE [PrescriptionTemplates]
        DROP CONSTRAINT [FK_PrescriptionTemplates_Doctors_DoctorId];
    PRINT N'• قید کلید خارجی الزامی پزشکِ تمپلیت نسخه حذف شد';
END

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'PrescriptionTemplates')
      AND name = N'DoctorId' AND is_nullable = 0)
BEGIN
    ALTER TABLE [PrescriptionTemplates] ALTER COLUMN [DoctorId] bigint NULL;
    PRINT N'• ستون DoctorId جدول PrescriptionTemplates اختیاری شد';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_PrescriptionTemplates_Doctors_DoctorId'
      AND parent_object_id = OBJECT_ID(N'PrescriptionTemplates'))
BEGIN
    ALTER TABLE [PrescriptionTemplates]
        ADD CONSTRAINT [FK_PrescriptionTemplates_Doctors_DoctorId]
        FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]);
    PRINT N'• قید کلید خارجی اختیاری پزشکِ تمپلیت نسخه بازافزوده شد';
END

-- ─── ۴) پادآرینی چرخه ۲۱ — ستون Items تمپلیت نسخه nvarchar(max) ───
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'PrescriptionTemplates')
      AND name = N'Items' AND max_length <> -1)
BEGIN
    ALTER TABLE [PrescriptionTemplates] ALTER COLUMN [Items] nvarchar(max) NULL;
    PRINT N'• ستون Items تمپلیت نسخه به nvarchar(max) آزاد شد';
END

PRINT N'✔ هات‌فیکس کامل شد — ستون‌های خدمت درمانی اکنون موجودند.';
PRINT N'  نکته: جدول __EFMigrationsHistory عمداً دست‌نخورده ماند؛ سرورِ اصلاح‌شده پس از ری‌استارت، مهاجرت‌های معلق را خودکار ثبت می‌کند (SQL گاردشده و بی‌اثر است).';
