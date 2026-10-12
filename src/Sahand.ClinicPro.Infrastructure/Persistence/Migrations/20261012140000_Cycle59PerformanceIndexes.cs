using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ ۵۹ (P-02 نقشه راه بازبینی مستقل ai-review) — ایندکس‌های ترکیبی
    /// سه پرس‌وجوی داغ کلینیک که تا امروز Full Scan یا Key Lookup + Sort داشتند:
    ///
    /// ۱) «پذیرش‌های امروز» (صف پذیرش + آمار صف — پرمصرف‌ترین صفحهٔ برنامه):
    ///    هیچ ایندکسی روی ReceptionDate نبود؛ ترکیب (ReceptionDate, Status)
    ///    بازه‌یابی روز + پالایش وضعیت را به Index Seek تبدیل می‌کند.
    /// ۲) میزکار پزشک/فهرست پذیرش هر پزشک در یک روز: (DoctorId, ReceptionDate)
    ///    به‌جای جست‌وجوی مجزای DoctorId و مرتب‌سازی تاریخ.
    /// ۳) صورتحساب بیمار به‌ترتیب تاریخ (صندوق/پرونده): (PatientId, InvoiceDate)
    ///    به‌جای دو ایندکس مجزا با Key Lookup در هر ردیف.
    ///
    /// سبک: دست‌نویسِ هم‌الگوی چرخه‌های ۲۱/۲۴/۳۹ (خانوادهٔ بدون Designer) با
    /// ویژگی‌های [DbContext]/[Migration] — گارد کشفِ چرخهٔ ۲۵ و فهرست طلاییِ
    /// چرخهٔ ۵۴ این را پوشش می‌دهند. پیکربندی هم‌زمان در ReceptionConfiguration
    /// و InvoiceConfiguration اعمال شد و ModelSnapshot هم به‌روز شد تا
    /// مهاجرت‌های تولیدیِ آتی همین ایندکس‌ها را دوباره نسازند.
    /// Idempotent نیست اما CreateIndex روی ستون‌های موجودِ بدون تغییر DDL است —
    /// امن روی هر حجم داده (عمل metadata-only در SQL Server، بدون بازسازی جدول).
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261012140000_Cycle59PerformanceIndexes")]
    public partial class Cycle59PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ۱) صف پذیرش — «امروز» + وضعیت
            migrationBuilder.CreateIndex(
                name: "IX_Reception_ReceptionDate_Status",
                table: "Receptions",
                columns: new[] { "ReceptionDate", "Status" });

            // ۲) میزکار پزشک — پزشک + بازهٔ روز
            migrationBuilder.CreateIndex(
                name: "IX_Reception_DoctorId_ReceptionDate",
                table: "Receptions",
                columns: new[] { "DoctorId", "ReceptionDate" });

            // ۳) صورتحساب بیمار به‌ترتیب تاریخ
            migrationBuilder.CreateIndex(
                name: "IX_Invoice_PatientId_InvoiceDate",
                table: "Invoices",
                columns: new[] { "PatientId", "InvoiceDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Invoice_PatientId_InvoiceDate", table: "Invoices");
            migrationBuilder.DropIndex(name: "IX_Reception_DoctorId_ReceptionDate", table: "Receptions");
            migrationBuilder.DropIndex(name: "IX_Reception_ReceptionDate_Status", table: "Receptions");
        }
    }
}
