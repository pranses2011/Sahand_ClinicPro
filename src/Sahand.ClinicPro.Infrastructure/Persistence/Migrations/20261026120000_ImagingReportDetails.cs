using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// ماژول حرفه‌ای تصویربرداری (طرح تأییدشدهٔ مالک، نسخهٔ ۲ پیش‌نمایش) —
    /// ستون ReportDetails (JSON) به جدول ImagingOrders اضافه می‌شود تا جزئیات
    /// ساخت‌یافتهٔ گزارش نگه داشته شود: دوز تابش (CTDIvol/DLP)، مقایسه با
    /// مطالعهٔ قبلی، وضعیت گزارش (پیش‌نویس/اولیه/نهایی/الحاقیه)، قالب
    /// استفاده‌شده، اندازه‌گیری‌های روی تصویر، چک‌لیست ایمنی انجام و
    /// اطلاع‌رسانی یافتهٔ بحرانی (گیرنده/روش/زمان). ستون‌های متن اصلی گزارش
    /// (Technique/Findings/Impression/Recommendation) از پیش وجود داشتند.
    ///
    /// سبک مهاجرت: SQL خامِ گاردشده با EXISTS (هم‌سبک Cycle39) تا روی هر نصب
    /// و هر وضعیت قبلی بی‌خطا و تکرارپذیر (idempotent) اجرا شود.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261026120000_ImagingReportDetails")]
    public partial class ImagingReportDetails : Migration
    {
        /// <summary>اجرای مهاجرت — افزودن ستون ReportDetails در صورت نبود</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'ImagingOrders') AND name = N'ReportDetails')
BEGIN
    ALTER TABLE [ImagingOrders] ADD [ReportDetails] nvarchar(max) NULL;
END
");
        }

        /// <summary>بازگشت — حذف ستون ReportDetails در صورت وجود</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'ImagingOrders') AND name = N'ReportDetails')
BEGIN
    ALTER TABLE [ImagingOrders] DROP COLUMN [ReportDetails];
END
");
        }
    }
}
