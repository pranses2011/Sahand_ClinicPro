using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle43StocktakingScope : Migration
    {
        /// <summary>
        /// اجرای مهاجرت — دو عملیات:
        /// ۱) پاک‌سازی تاریخی KeysJson و الزامی‌کردن آن فقط اگر جدول از قبل
        ///    وجود داشته و ستون تهی‌پذیر باشد؛ در دیتابیس تازه Cycle43 بر اساس
        ///    شناسهٔ migration پیش از Cycle41 اجرا می‌شود، پس جدول هنوز نیست و
        ///    Cycle41 آن را از ابتدا با ستون NOT NULL می‌سازد؛
        /// ۲) افزودن ستون ItemKind به StockTakings — دامنهٔ جلسهٔ انبارگردانی
        ///    (چرخهٔ ۴۳: دارو / لوازم مصرفی / کل انبار).
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration ID این فایل از Cycle41UserShortcuts قدیمی‌تر است؛ پس در
            // دیتابیس خالی EF این migration را زودتر اجرا می‌کند. در آن حالت جدول
            // هنوز ساخته نشده و Cycle41 بعدتر آن را از ابتدا با KeysJson NOT NULL
            // می‌سازد. در دیتابیس‌های قبلی که جدول از قبل وجود دارد، NULLها پاک و
            // ستون به NOT NULL تبدیل می‌شود. گارد برای هر دو ترتیب امن است.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[UserShortcuts]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.UserShortcuts', N'KeysJson') IS NOT NULL
BEGIN
    UPDATE [dbo].[UserShortcuts]
    SET [KeysJson] = N'[]'
    WHERE [KeysJson] IS NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[UserShortcuts]')
          AND name = N'KeysJson'
          AND is_nullable = 1)
    BEGIN
        ALTER TABLE [dbo].[UserShortcuts]
        ALTER COLUMN [KeysJson] nvarchar(4000) NOT NULL;
    END
END
");

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

            // هنگام rollback هم ممکن است Cycle41 پیش‌تر جدول را حذف کرده باشد؛
            // فقط وقتی جدول/ستون موجود و غیرتهی‌پذیر است، وضعیت قبلی را برگردان.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[UserShortcuts]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.UserShortcuts', N'KeysJson') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[UserShortcuts]')
          AND name = N'KeysJson'
          AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[UserShortcuts]
    ALTER COLUMN [KeysJson] nvarchar(4000) NULL;
END
");
        }
    }
}
