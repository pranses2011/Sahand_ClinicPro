using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ ۶۹ (فاز ۳ / F-03 «یادآوری خودکار نوبت») — ستون پرچم موج دوم یادآوری.
    ///
    /// Appointment.ReminderSent از قبل پرچم پیامک ۲۴ساعته را نگه می‌داشت؛
    /// نقشه راه F-03 دو موج می‌خواهد (۲۴ ساعت + ۲ ساعت) با پرچم مستقل تا بیمار
    /// هر دو یادآوری را دریافت کند و تکرار هیچ‌کدام ممکن نباشد. ستون
    /// Reminder2Sent (bit، non-null با پیش‌فرض ۰) همان نقش را برای موج دوم دارد.
    ///
    /// سبک: دست‌نویسِ هم‌الگوی خانوادهٔ بدون Designer (چرخه‌های ۲۱/۲۴/۳۹/۵۹) با
    /// ویژگی‌های [DbContext]/[Migration] — گارد کشف چرخهٔ ۲۵ و فهرست طلایی چرخهٔ ۵۴
    /// این را پوشش می‌دهند. Idempotent نیست اما AddColumn با defaultValue روی
    /// جدول موجود metadata-only است — امن روی هر حجم داده، بدون بازسازی جدول.
    /// ModelSnapshot هم‌زمان به‌روز شد تا مایگریشن‌های تولیدی آینده ستون را
    /// دوباره نسازند.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261014090000_Cycle69AutoReminders")]
    public partial class Cycle69AutoReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // پرچم «پیامک یادآوری ۲ ساعت قبل ارسال شده» — مستقل از موج ۲۴ ساعته
            migrationBuilder.AddColumn<bool>(
                name: "Reminder2Sent",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reminder2Sent",
                table: "Appointments");
        }
    }
}
