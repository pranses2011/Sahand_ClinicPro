-- ═══════════════════════════════════════════════════════════════════════
-- اسکریپت نگهداشت آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷)
-- سامانه جامع مدیریت درمانگاه سهند | Sahand ClinicPro
--
-- کاربرد: پس از اجرای آرشیو از پنل مدیریت (یا زمان‌بندی‌شده)، این اسکریپت
-- در بازه‌های نگهداشت اجرا می‌شود تا:
--   ۱) ردیف‌های حذف‌نرم‌شده‌ای که رکورد آرشیو دارند فیزیکی پاک شوند
--   ۲) ایندکس‌های جدول عملیاتی بازسازی و آمار به‌روز شود
--   ۳) ایندکس‌های جدول آرشیو بهینه بمانند
--
-- هشدار: پیش از اجرای بخش حذف فیزیکی، از بانک پشتیبان بگیرید.
-- ═══════════════════════════════════════════════════════════════════════

USE SahandClinicProDb;
GO

-- ─── ۱) پاک‌سازی فیزیکی ردیف‌های آرشیوشده (حذف نرم + رکورد آرشیو موجود) ───
-- فقط ردیف‌هایی حذف می‌شوند که تصویر کاملشان در ArchiveRecords ثبت شده است.

DELETE FROM Payments
WHERE IsDeleted = 1
  AND EXISTS (SELECT 1 FROM ArchiveRecords a
              WHERE a.SourceTable = 'Payments' AND a.SourceId = Payments.Id AND a.IsDeleted = 0);

DELETE FROM Invoices
WHERE IsDeleted = 1
  AND EXISTS (SELECT 1 FROM ArchiveRecords a
              WHERE a.SourceTable = 'Invoices' AND a.SourceId = Invoices.Id AND a.IsDeleted = 0);

DELETE FROM Prescriptions
WHERE IsDeleted = 1
  AND EXISTS (SELECT 1 FROM ArchiveRecords a
              WHERE a.SourceTable = 'Prescriptions' AND a.SourceId = Prescriptions.Id AND a.IsDeleted = 0);

DELETE FROM LabOrders
WHERE IsDeleted = 1
  AND EXISTS (SELECT 1 FROM ArchiveRecords a
              WHERE a.SourceTable = 'LabOrders' AND a.SourceId = LabOrders.Id AND a.IsDeleted = 0);

DELETE FROM Visits
WHERE IsDeleted = 1
  AND EXISTS (SELECT 1 FROM ArchiveRecords a
              WHERE a.SourceTable = 'Visits' AND a.SourceId = Visits.Id AND a.IsDeleted = 0);

DELETE FROM Receptions
WHERE IsDeleted = 1
  AND EXISTS (SELECT 1 FROM ArchiveRecords a
              WHERE a.SourceTable = 'Receptions' AND a.SourceId = Receptions.Id AND a.IsDeleted = 0);

DELETE FROM Appointments
WHERE IsDeleted = 1
  AND EXISTS (SELECT 1 FROM ArchiveRecords a
              WHERE a.SourceTable = 'Appointments' AND a.SourceId = Appointments.Id AND a.IsDeleted = 0);
GO

-- ─── ۲) بازسازی ایندکس‌های جدول‌های عملیاتی سبک‌شده ───
-- پس از حذف انبوه، ایندکس‌ها تکه‌تکه می‌شوند؛ REORGANIZE سبک است و آنلاین اجرا می‌شود.

ALTER INDEX IX_Appointments_PatientId ON Appointments   REORGANIZE;
ALTER INDEX IX_Visits_PatientId       ON Visits         REORGANIZE;
ALTER INDEX IX_Invoices_PatientId     ON Invoices       REORGANIZE;
ALTER INDEX IX_Payments_PatientId     ON Payments       REORGANIZE;
ALTER INDEX IX_Receptions_PatientId   ON Receptions     REORGANIZE;
GO

-- در بازه‌های نگهداشت کم‌ترراکنش (مثلاً جمعه‌بامداد) یک‌بار REBUILD کامل:
-- ALTER INDEX ALL ON Appointments REBUILD WITH (ONLINE = ON);
-- ALTER INDEX ALL ON Visits       REBUILD WITH (ONLINE = ON);
-- ALTER INDEX ALL ON Invoices     REBUILD WITH (ONLINE = ON);

-- ─── ۳) به‌روزرسانی آمار کوئری‌پلنر ───
UPDATE STATISTICS Appointments WITH FULLSCAN;
UPDATE STATISTICS Visits       WITH FULLSCAN;
UPDATE STATISTICS Invoices     WITH FULLSCAN;
UPDATE STATISTICS Payments     WITH FULLSCAN;
UPDATE STATISTICS Receptions   WITH FULLSCAN;
UPDATE STATISTICS ArchiveRecords WITH FULLSCAN;
GO

-- ─── ۴) شاخص‌های سلامت آرشیو ───
-- گزارش حجم جداول عملیاتی پس از آرشیو:
SELECT 'Appointments' AS TableName, COUNT(*) AS Rows, SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS SoftDeleted FROM Appointments
UNION ALL SELECT 'Visits', COUNT(*), SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) FROM Visits
UNION ALL SELECT 'Invoices', COUNT(*), SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) FROM Invoices
UNION ALL SELECT 'Payments', COUNT(*), SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) FROM Payments
UNION ALL SELECT 'Receptions', COUNT(*), SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) FROM Receptions
UNION ALL SELECT 'ArchiveRecords', COUNT(*), SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) FROM ArchiveRecords;
GO
