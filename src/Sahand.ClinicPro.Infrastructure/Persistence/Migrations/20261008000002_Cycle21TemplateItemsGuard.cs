using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخه بیست‌ویکم (گزارش پانزدهم مالک): «ذخیره به‌عنوان تمپلیت باز هم خطای
    /// "خطای غیر منتظره در سرور رخ داد" می‌دهد» — ریشه: ستون Items جدول
    /// PrescriptionTemplates در برخی دیتابیس‌های نصب‌شده هنوز nvarchar(200) است و
    /// JSON اقلام تمپلیت (چند دارو) طولش از ۲۰۰ کاراکتر می‌گذرد → SqlExceptionِ
    /// truncation → خطای ۵۰۰.
    /// این مهاجرت با SQL خامِ شرطی و idempotent ستون را تضمینی nvarchar(max)
    /// می‌کند؛ چه مهاجرت چرخه ۲۰ قبلاً اعمال شده باشد چه به‌دلایل تاریخچهٔ
    /// مهاجرت گیر کرده باشد. (attribute زمان‌دار تا پس از همهٔ مهاجرات اجرا شود)
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261008000002_Cycle21TemplateItemsGuard")]
    public partial class Cycle21TemplateItemsGuard : Migration
    {
        /// <summary>تغییر شرطی نوع ستون به nvarchar(max) اگر هنوز نیست</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // max_length = -1 یعنی nvarchar(max)؛ اگر ستون هنوز محدود است، آزادش کن
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'PrescriptionTemplates')
      AND name = N'Items'
      AND max_length <> -1)
BEGIN
    ALTER TABLE PrescriptionTemplates ALTER COLUMN Items nvarchar(max) NULL;
END
");
        }

        /// <summary>بازگشت به حالت قبل لازم نیست — ستون max بی‌ضرر است</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // عمداً خالی: کوچک‌کردن ستون می‌تواند داده‌های بلند را از بین ببرد
        }
    }
}
