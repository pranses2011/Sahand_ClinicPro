using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle43StocktakingScope : Migration
    {
        /// <summary>
        /// اجرای مهاجرت — دو عملیات:
        /// ۱) نال‌پاکسازی KeysJson جدول UserShortcuts و الزامی‌کردن آن —
        ///    مهاجرت دست‌نویس چرخهٔ ۴۱ ستون را تهی‌پذیر ساخته بود ولی اسنپ‌شات
        ///    مدل آن را الزامی می‌داند؛ ردیف‌های نال (نباید وجود داشته باشند —
        ///    Upsert همیشه مقدار می‌نویسد) با «[]» پر می‌شوند تا ALTER بی‌خطا
        ///    اجرا شود (گارد idempotent — روی دیتابیس بدون نال بی‌اثر است)؛
        /// ۲) افزودن ستون ItemKind به StockTakings — دامنهٔ جلسهٔ انبارگردانی
        ///    (چرخهٔ ۴۳: دارو / لوازم مصرفی / کل انبار).
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // نال‌پاکسازی پیش از الزامی‌کردن — در دیتابیس‌های موجود که ستون
            // تهی‌پذیر ساخته شده، ردیف نال در صورت وجود با آرایهٔ خالی پر می‌شود
            migrationBuilder.Sql(@"UPDATE [UserShortcuts] SET [KeysJson] = '[]' WHERE [KeysJson] IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "KeysJson",
                table: "UserShortcuts",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]",
                comment: "کلید صفحات پین‌شدهٔ داشبورد به‌صورت JSON آرایه‌ای",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true,
                oldDefaultValue: "[]",
                oldComment: "کلید صفحات پین‌شدهٔ داشبورد به‌صورت JSON آرایه‌ای");

            // دامنهٔ جلسهٔ انبارگردانی — تهی برای جلسه‌های قدیمی = کل انبار (رفتار پیشین)
            migrationBuilder.AddColumn<byte>(
                name: "ItemKind",
                table: "StockTakings",
                type: "tinyint",
                nullable: true,
                comment: "دامنهٔ جلسه انبارگردانی — ۱: دارو، ۲: لوازم مصرفی، تهی: کل انبار — چرخهٔ ۴۳");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemKind",
                table: "StockTakings");

            migrationBuilder.AlterColumn<string>(
                name: "KeysJson",
                table: "UserShortcuts",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                defaultValue: "[]",
                comment: "کلید صفحات پین‌شدهٔ داشبورد به‌صورت JSON آرایه‌ای",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldDefaultValue: "[]",
                oldComment: "کلید صفحات پین‌شدهٔ داشبورد به‌صورت JSON آرایه‌ای");
        }
    }
}
