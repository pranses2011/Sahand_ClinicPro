using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// چرخهٔ سی‌ودوم (گزارش مالک) — دو اصلاح هم‌زمان:
    /// ۱) دقت ستون‌های مبلغ ریالی «سهم بیمه» و «سهم بیمار» جدول اقلام فاکتور
    ///    از decimal(5,2) (سقف ۹۹۹٫۹۹!) به decimal(18,2) ارتقا می‌یابد؛ ریشهٔ
    ///    خطای ۵۰۰ «خطای غیرمنتظره» هنگام «پذیرش و پرداخت نقدی» و بی‌نتیجه
    ///    ماندن کل زنجیرهٔ مالی بود (SqlException: Arithmetic overflow).
    /// ۲) دادهٔ تعرفه‌های «آزاد (بدون بیمه)» یک‌بار برای همیشه به قاعدهٔ درست
    ///    بازتنظیم می‌شود: کل مبلغ بر عهدهٔ بیمار — درصد بیمه ۰ و درصد بیمار ۱۰۰.
    ///    بذر اولیه این ستون‌ها را از «درصد پوشش بیمهٔ پیش‌فرض» (مثلاً ۷۰/۳۰)
    ///    پر می‌کرد که برای ردیف آزاد بی‌معناست.
    /// </summary>
    public partial class Cycle32FreeTariffAndSharePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── ۱) گسترش ستون‌های مبلغ سهم اقلام فاکتور ──
            migrationBuilder.AlterColumn<decimal>(
                name: "InsuranceShare",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "سهم ریالی بیمه از این قلم",
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldComment: "سهم بیمه");

            migrationBuilder.AlterColumn<decimal>(
                name: "PatientShare",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "سهم ریالی بیمار از این قلم",
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldComment: "سهم بیمار");

            // ── ۲) اصلاح دادهٔ تعرفه‌های آزاد: بیمه‌ای در کار نیست ──
            // همهٔ ردیف‌های تعرفهٔ بدون بیمه (InsuranceCompanyId تهی) باید
            // ۱۰۰٪ سهم بیمار و ۰٪ سهم بیمه داشته باشند؛ مقادیر بیمه‌ای قدیمی
            // (۷۰/۳۰ از بذر اولیه) در پذیرش و لیست تعرفه‌ها سهم بیمهٔ غیرواقعی
            // می‌ساخت. شرط سطرِ تکراری اجرا هم بی‌ضرر است (idempotent).
            migrationBuilder.Sql(
                "UPDATE Tariffs SET InsuranceShare = 0, PatientShare = 100 " +
                "WHERE InsuranceCompanyId IS NULL AND IsDeleted = 0 " +
                "AND (ISNULL(InsuranceShare, 0) <> 0 OR ISNULL(PatientShare, 0) <> 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // بازگشت دقت ستون‌ها به حالت پیشین — فقط در صورت لغو کامل مهاجرت؛
            // مقادیر بزرگ‌تر از ۹۹۹٫۹۹ با خطای Overflow مواجه می‌شوند که خودش
            // نشان‌دهندهٔ نادرستی دقت قدیمی است.
            migrationBuilder.AlterColumn<decimal>(
                name: "InsuranceShare",
                table: "InvoiceItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                comment: "سهم بیمه",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "سهم ریالی بیمه از این قلم");

            migrationBuilder.AlterColumn<decimal>(
                name: "PatientShare",
                table: "InvoiceItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                comment: "سهم بیمار",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "سهم ریالی بیمار از این قلم");
        }
    }
}
