using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ سی‌ونه‌م (گزارش مالک) — نوع قلم انبار داروخانه:
    /// «لوازمی که استفاده می‌شود مثل سرنگ و سرم و گاز استریل و مواردی از این
    /// قبیل رو وقتی بیمار با خودش نمیاره، باید از لوازم خود درمانگاه استفاده
    /// بکنیم و در پذیرش برای اونها هم هزینه به هزینه‌های بیمار اضافه بشه و خرید
    /// و انبار و قیمت‌های این لوازم هم باید باشه» — به‌جای ماژول موازی، همان
    /// جدول Medications با ستون ItemKind به دو دسته تقسیم می‌شود: دارو (۱) و
    /// لوازم مصرفی (۲). مقدار پیش‌فرض ۱ یعنی همهٔ ردیف‌های موجود پس از مهاجرت
    /// بدون تغییر «دارو» باقی می‌مانند و هیچ رفتار پیشین نمی‌شکند؛ اقلام مصرفیِ
    /// جدید از اندپوینت POST lookups/medications با نوع ۲ ساخته می‌شوند.
    ///
    /// سبک مهاجرت: SQL خامِ گاردشده با EXISTS (هم‌سبک Cycle24/۳۹-اتاق) تا روی
    /// هر نصب و هر وضعیت ستون قبلی، تکرارپذیر (idempotent) اجرا شود.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261011130000_Cycle39MedicationItemKind")]
    public partial class Cycle39MedicationItemKind : Migration
    {
        /// <summary>
        /// اجرای مهاجرت — افزودن ستون ItemKind با مقدار پیش‌فرض ۱ (دارو).
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Medications') AND name = N'ItemKind')
BEGIN
    ALTER TABLE [Medications]
        ADD [ItemKind] tinyint NOT NULL
        CONSTRAINT [DF_Medications_ItemKind] DEFAULT 1
        WITH VALUES;

    EXEC sp_addextendedproperty
        @name = N'MS_Description',
        @value = N'نوع قلم انبار — ۱: دارو، ۲: لوازم مصرفی (سرنگ/سرم/گاز و…) — چرخهٔ ۳۹',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'Medications',
        @level2type = N'COLUMN', @level2name = N'ItemKind';
END
");
        }

        /// <summary>
        /// بازگشت — اگر قلم مصرفی (نوع ۲) ثبت شده باشد، ستون به ۱ (دارو)
        /// فرومی‌نشیند تا نقض قید نکند؛ سپس قید پیش‌فرض و ستون حذف می‌شوند.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Medications') AND name = N'ItemKind')
BEGIN
    EXEC sp_dropextendedproperty
        @name = N'MS_Description',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'Medications',
        @level2type = N'COLUMN', @level2name = N'ItemKind';

    IF EXISTS (
        SELECT 1 FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id
        WHERE dc.name = N'DF_Medications_ItemKind'
          AND c.name = N'ItemKind' AND c.object_id = OBJECT_ID(N'Medications'))
    BEGIN
        ALTER TABLE [Medications] DROP CONSTRAINT [DF_Medications_ItemKind];
    END

    ALTER TABLE [Medications] DROP COLUMN [ItemKind];
END
");
        }
    }
}
