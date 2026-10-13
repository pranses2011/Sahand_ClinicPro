using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ ۶۹ (فاز ۳ / F-18 «برچسب بیمار (VIP/مزمن)») — ستون Tags روی Patients.
    ///
    /// CSV فارسی مثل «VIP، دیابت، فشارخون» — چیپ‌های رنگی روی پرونده و فهرست
    /// بیماران؛ پیشنهادهای آماده VIP/مزمن/باردار + دلخواه. تهی = بدون برچسب.
    ///
    /// سبک: دست‌نویسِ هم‌الگوی خانوادهٔ بدون Designer (چرخه‌های ۲۱/۲۴/۳۹/۵۹/۶۹-قبلی)
    /// با ویژگی‌های [DbContext]/[Migration] — گارد کشف چرخهٔ ۲۵ و فهرست طلایی
    /// چرخهٔ ۵۴ پوشش می‌دهند. AddColumn تهی metadata-only است — امن روی هر حجم
    /// داده. ModelSnapshot هم‌زمان به‌روز شد. PatientConfiguration هم همین ستون
    /// را با پیکربندی کامل (طول + کامنت) اعلام می‌کند.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261014100000_Cycle69PatientTags")]
    public partial class Cycle69PatientTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Patients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "برچسب‌های بیمار CSV — چرخهٔ ۶۹ (F-18)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Patients");
        }
    }
}
