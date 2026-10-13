using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ ۶۹ (فاز ۳ / F-28 «محدودهٔ مرجع سنی/جنسیتی» + F-29 «مقادیر بحرانی») —
    /// نُه ستون ساخت‌یافته روی کاتالوگ LabTests.
    ///
    /// موتور تفسیر خودکار نتیجه (LabResultInterpreter) به بازه‌های عددی نیاز دارد؛
    /// ستون‌های رشته‌ای قدیمی (NormalRangeMale/Female) برای نمایش عمومی می‌مانند:
    /// ۶ ستون بازهٔ مرجع (کف/سقف × مرد/زن/کودک) + سقف سن کودک + ۲ حد بحرانی.
    /// همه تهی‌پذیرند — تست بدون بازه، تفسیر نمی‌شود (نه طبیعی نه غیرطبیعی)؛
    /// پس این مایگریشن روی هیچ دادهٔ موجودی اثر رفتاری ندارد تا مدیر آزمایشگاه
    /// بازه‌ها را از «تنظیمات ← آزمایش‌ها» وارد کند.
    ///
    /// سبک: دست‌نویسِ هم‌الگوی خانوادهٔ بدون Designer (چرخه‌های ۲۱/۲۴/۳۹/۵۹/۶۹-نوبت)
    /// با ویژگی‌های [DbContext]/[Migration] — گارد کشف چرخهٔ ۲۵ و فهرست طلایی
    /// چرخهٔ ۵۴ پوشش می‌دهند. AddColumn با تهی روی جدول موجود metadata-only است —
    /// امن روی هر حجم داده. ModelSnapshot هم‌زمان به‌روز شد.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261014093000_Cycle69LabReferenceRanges")]
    public partial class Cycle69LabReferenceRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── بازه‌های مرجع بزرگسال — به تفکیک جنسیت (F-28) ──
            migrationBuilder.AddColumn<decimal>(
                name: "RefMinAdultMale",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "کف بازهٔ مرجع بزرگسال مرد — چرخهٔ ۶۹ (F-28)");

            migrationBuilder.AddColumn<decimal>(
                name: "RefMaxAdultMale",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "سقف بازهٔ مرجع بزرگسال مرد — چرخهٔ ۶۹ (F-28)");

            migrationBuilder.AddColumn<decimal>(
                name: "RefMinAdultFemale",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "کف بازهٔ مرجع بزرگسال زن — چرخهٔ ۶۹ (F-28)");

            migrationBuilder.AddColumn<decimal>(
                name: "RefMaxAdultFemale",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "سقف بازهٔ مرجع بزرگسال زن — چرخهٔ ۶۹ (F-28)");

            // ── بازهٔ مرجع کودک + سقف سن (F-28) ──
            migrationBuilder.AddColumn<decimal>(
                name: "RefMinChild",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "کف بازهٔ مرجع کودک — چرخهٔ ۶۹ (F-28)");

            migrationBuilder.AddColumn<decimal>(
                name: "RefMaxChild",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "سقف بازهٔ مرجع کودک — چرخهٔ ۶۹ (F-28)");

            migrationBuilder.AddColumn<int>(
                name: "ChildAgeMaxYears",
                table: "LabTests",
                type: "int",
                nullable: true,
                comment: "سقف سن کودک به سال — چرخهٔ ۶۹ (F-28)");

            // ── حدود بحرانی — دیالوگ مسدودکنندهٔ هشدار پزشک (F-29) ──
            migrationBuilder.AddColumn<decimal>(
                name: "CriticalLow",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "کف مقدار بحرانی — چرخهٔ ۶۹ (F-29)");

            migrationBuilder.AddColumn<decimal>(
                name: "CriticalHigh",
                table: "LabTests",
                type: "decimal(18,4)",
                nullable: true,
                comment: "سقف مقدار بحرانی — چرخهٔ ۶۹ (F-29)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RefMinAdultMale", table: "LabTests");
            migrationBuilder.DropColumn(name: "RefMaxAdultMale", table: "LabTests");
            migrationBuilder.DropColumn(name: "RefMinAdultFemale", table: "LabTests");
            migrationBuilder.DropColumn(name: "RefMaxAdultFemale", table: "LabTests");
            migrationBuilder.DropColumn(name: "RefMinChild", table: "LabTests");
            migrationBuilder.DropColumn(name: "RefMaxChild", table: "LabTests");
            migrationBuilder.DropColumn(name: "ChildAgeMaxYears", table: "LabTests");
            migrationBuilder.DropColumn(name: "CriticalLow", table: "LabTests");
            migrationBuilder.DropColumn(name: "CriticalHigh", table: "LabTests");
        }
    }
}
