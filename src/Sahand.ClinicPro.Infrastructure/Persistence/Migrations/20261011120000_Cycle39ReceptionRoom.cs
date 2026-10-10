using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ سی‌ونه‌م (گزارش مالک) — اتاق انجام خدمت در پذیرش خدمتی:
    /// «اتاق معاینه برای پزشک در برنامهٔ کاری پزشک مشخص می‌شود ولی برای خدمات
    /// درمانی نمی‌شود معین کرد» — ستون RoomId به جدول Receptions اضافه می‌شود
    /// تا در پذیرش خدمتی (تزریقات/پانسمان/سرم و مانند آن) اتاقِ محل انجام خدمت
    /// از فهرست اتاق‌های فعال درمانگاه انتخاب و ثبت شود؛ بیمار به اتاق درست
    /// هدایت می‌شود و بار اتاق‌ها در طول روز قابل ردیابی است. برای پذیرش
    /// ویزیت پزشک ستون تهی می‌ماند (اتاق از برنامهٔ کاری خود پزشک می‌آید).
    ///
    /// سبک مهاجرت: SQL خامِ گاردشده با EXISTS (هم‌سبک Cycle24) تا روی هر نصب
    /// و هر وضعیت قید قبلی بی‌خطا و تکرارپذیر (idempotent) اجرا شود.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261011120000_Cycle39ReceptionRoom")]
    public partial class Cycle39ReceptionRoom : Migration
    {
        /// <summary>
        /// اجرای مهاجرت — افزودن ستون RoomId + کلید خارجی + ایندکس، هر سه با
        /// گارد شرطی تا اجرای مکرر بی‌ضرر باشد.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── گام ۱: ستون RoomId (در صورت نبود) ───
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'RoomId')
BEGIN
    ALTER TABLE [Receptions] ADD [RoomId] bigint NULL;
END
");

            // ─── گام ۲: کلید خارجی به ExaminationRooms (NoAction — حذف اتاق
            //     هرگز پذیرش تاریخی را نمی‌کشد؛ اتاق‌ها فقط نرم-غیرفعال می‌شوند) ───
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Receptions_ExaminationRooms_RoomId'
      AND parent_object_id = OBJECT_ID(N'Receptions'))
BEGIN
    ALTER TABLE [Receptions]
        ADD CONSTRAINT [FK_Receptions_ExaminationRooms_RoomId]
        FOREIGN KEY ([RoomId]) REFERENCES [ExaminationRooms] ([Id])
        ON DELETE NO ACTION;
END
");

            // ─── گام ۳: ایندکس پرس‌وجوهای اتاق (بار اتاق در طول روز) —
            //     ساده و بدون فیلتر، هم‌سبک ایندکس‌های کانونشی EF که اسنپ‌شات
            //     نیز همان را توصیف می‌کند (b.HasIndex("RoomId")) ───
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Receptions_RoomId'
      AND object_id = OBJECT_ID(N'Receptions'))
BEGIN
    CREATE INDEX [IX_Receptions_RoomId]
    ON [Receptions] ([RoomId]);
END
");
        }

        /// <summary>
        /// بازگشت — حذف ایندکس، قید و ستون اتاق؛ ترتیب حذف (ایندکس ← قید ← ستون)
        /// الزام SQL Server است. اجرای گاردشده تا روی وضعیت‌های میانی هم بی‌خطا بماند.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Receptions_RoomId'
      AND object_id = OBJECT_ID(N'Receptions'))
BEGIN
    DROP INDEX [IX_Receptions_RoomId] ON [Receptions];
END

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Receptions_ExaminationRooms_RoomId'
      AND parent_object_id = OBJECT_ID(N'Receptions'))
BEGIN
    ALTER TABLE [Receptions]
        DROP CONSTRAINT [FK_Receptions_ExaminationRooms_RoomId];
END

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Receptions') AND name = N'RoomId')
BEGIN
    ALTER TABLE [Receptions] DROP COLUMN [RoomId];
END
");
        }
    }
}
