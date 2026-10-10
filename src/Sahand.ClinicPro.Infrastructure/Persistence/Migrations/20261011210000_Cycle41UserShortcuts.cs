using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ چهل‌ویکم (گزارش مالک) — جدول میانبرهای سریع داشبورد کاربر:
    /// «پنل میانبرهای سریع داشبورد در دیتابیس ذخیره بشه تا مشکلی براش پیش نیاد»
    /// — پیش از این مهاجرت، چیدمان شخصی هر کاربر فقط در فایل محلی
    /// %AppData%/SahandClinicPro/shortcuts.{username}.json زندگی می‌کرد؛ تعویض
    /// ایستگاه/پروفایل ویندوز آن را می‌برد و پشتیبان سرور چیزی از آن نگه
    /// نمی‌داشت. از این پس هر کاربر حداکثر یک ردیف در UserShortcuts دارد و
    /// دسکتاپ فایل محلی را فقط به‌عنوان کشِ آفلاین و منبع مهاجرتِ یک‌بارهٔ
    /// چیدمان‌های قدیمی می‌خواند (رفتار پیشین کاملاً حفظ شده است).
    ///
    /// سبک مهاجرت: SQL خامِ گاردشده با OBJECT_ID (هم‌سبک Cycle24/۳۹) تا روی هر
    /// نصب و هر وضعیت قبلی، تکرارپذیر (idempotent) اجرا شود.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261011210000_Cycle41UserShortcuts")]
    public partial class Cycle41UserShortcuts : Migration
    {
        /// <summary>
        /// اجرای مهاجرت — ساخت جدول UserShortcuts + ایندکس یکتای فیلترشدهٔ UserId.
        /// ایندکس فقط روی ردیف‌های سالم (IsDeleted=0) یکتاست — همان الگوی
        /// IX_Waitlist_Status_Doctor — تا نرم‌حذفِ ردیف، درج مجدد برای همان
        /// کاربر را قفل نکند.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[UserShortcuts]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[UserShortcuts] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT [PK_UserShortcuts] PRIMARY KEY,
        [UserId] BIGINT NOT NULL,
        [KeysJson] NVARCHAR(4000) NOT NULL
            CONSTRAINT [DF_UserShortcuts_KeysJson] DEFAULT N'[]',
        [CreatedAt] DATETIME2 NOT NULL
            CONSTRAINT [DF_UserShortcuts_CreatedAt] DEFAULT GETUTCDATE(),
        [CreatedBy] BIGINT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] BIGINT NULL,
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_UserShortcuts_IsDeleted] DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] BIGINT NULL,
        [BranchId] BIGINT NULL,
        [RowVersion] ROWVERSION NOT NULL
    );

    EXEC sp_addextendedproperty
        @name = N'MS_Description',
        @value = N'میانبرهای سریع داشبورد کاربر — چرخهٔ ۴۱',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'UserShortcuts';

    EXEC sp_addextendedproperty
        @name = N'MS_Description',
        @value = N'کلید صفحات پین‌شدهٔ داشبورد به‌صورت JSON آرایه‌ای',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'UserShortcuts',
        @level2type = N'COLUMN', @level2name = N'KeysJson';

    CREATE UNIQUE INDEX [IX_UserShortcuts_UserId]
        ON [dbo].[UserShortcuts] ([UserId])
        WHERE [IsDeleted] = 0;
END
");
        }

        /// <summary>
        /// بازگشت — حذف ایندکس و جدول؛ دادهٔ شخصی و بازسازی‌پذیر است و مهاجرت
        /// معکوس نیازی به نگه‌داری آن ندارد (دسکتاپ در نبود رکورد سروری به فایل
        /// محلی/پیش‌فرض‌ها برمی‌گردد).
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[UserShortcuts]') AND type = N'U')
BEGIN
    DROP INDEX [IX_UserShortcuts_UserId] ON [dbo].[UserShortcuts];

    EXEC sp_dropextendedproperty
        @name = N'MS_Description',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'UserShortcuts',
        @level2type = N'COLUMN', @level2name = N'KeysJson';

    EXEC sp_dropextendedproperty
        @name = N'MS_Description',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'UserShortcuts';

    DROP TABLE [dbo].[UserShortcuts];
END
");
        }
    }
}
