using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخه بیست‌وسوم (گزارش شانزدهم مالک): «در نسخه‌نویسی تمپلیت را هنوز هم ذخیره
    /// نمی‌کند و خطای "خطای غیر منتظره در سرور رخ داد — با پشتیبانی تماس بگیرید"
    /// می‌دهد» — ریشهٔ اصلی (علاوه بر اصلاح nvarchar(max) چرخه ۲۰/۲۱):
    /// ستون DoctorId جدول PrescriptionTemplates «NOT NULL» با کلید خارجیِ الزامی به
    /// جدول Doctors بود. کلاینت دسکتاپ هنگام «ذخیره به‌عنوان تمپلیت» doctorId نمی‌فرستاد
    /// (یا پزشکی هنوز در فرم انتخاب نشده بود)؛ گردانندهٔ سرور مقدار پیش‌فرض صفر را
    /// درج می‌کرد و چون پزشکی با شناسهٔ صفر وجود ندارد، INSERT با نقض کلید خارجی
    /// (The INSERT statement conflicted with the FOREIGN KEY constraint) شکست
    /// می‌خورد → ExceptionHandlingMiddleware آن را به خطای ۵۰۰ «خطای غیرمنتظره در
    /// سرور رخ داد» ترجمه می‌کرد.
    ///
    /// رفع ساختاری در این مهاجرت:
    ///   ۱) حذف قید FK الزامی موجود (FK_PrescriptionTemplates_Doctors_DoctorId)
    ///   ۲) اختیاری‌کردن ستون DoctorId (NULL مجاز — یعنی تمپلیت عمومی بدون پزشک)
    ///   ۳) بازافزودن قید FK به‌صورت اختیاری
    /// همهٔ مراحل با SQL شرطی و idempotent نوشته شده‌اند تا روی هر نصبی — چه قید
    /// قبلی با این نام باشد چه نباشد — بدون خطا اجرا شوند. سمت کد هم موجودیت
    /// long? شد و کلاینت پزشکِ انتخابی را ارسال می‌کند.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261009000001_Cycle23TemplateDoctorNullable")]
    public partial class Cycle23TemplateDoctorNullable : Migration
    {
        /// <summary>
        /// حذف FK الزامی → اختیاری‌کردن ستون → بازافزودن FK اختیاری.
        /// شرط‌ها با EXISTS گارد شده‌اند تا اجرای دوباره بی‌خطا باشد.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── گام ۱: حذف قید کلید خارجی الزامی (اگر وجود داشته باشد) ───
            // نام قید از مهاجرت InitialCreate است؛ با EXISTS گارد می‌شود تا روی
            // دیتابیس‌هایی که قید را ندارند (یا نامش متفاوت است) خطا ندهد
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_PrescriptionTemplates_Doctors_DoctorId'
      AND parent_object_id = OBJECT_ID(N'PrescriptionTemplates'))
BEGIN
    ALTER TABLE [PrescriptionTemplates]
        DROP CONSTRAINT [FK_PrescriptionTemplates_Doctors_DoctorId];
END
");

            // ─── گام ۲: اختیاری‌کردن ستون DoctorId ───
            // ردیف‌های قبلی با مقدار 0 (ناشی از باگ قدیمی) به NULL تبدیل می‌شوند
            // تا قید FK جدید را نقض نکنند — تمپلیت‌های قدیمیِ «پزشک صفر» عمومی می‌شوند
            migrationBuilder.Sql(@"
UPDATE [PrescriptionTemplates] SET [DoctorId] = NULL WHERE [DoctorId] = 0;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'PrescriptionTemplates')
      AND name = N'DoctorId'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE [PrescriptionTemplates] ALTER COLUMN [DoctorId] bigint NULL;
END
");

            // ─── گام ۳: بازافزودن قید FK به‌صورت اختیاری (اگر قبلاً نباشد) ───
            // با ستونِ اکنونِ nullable، درج تمپلیت بدون پزشک (NULL) مجاز است و
            // درج با پزشک معتبر همچنان از سالمت ارجاع تضمین می‌شود
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_PrescriptionTemplates_Doctors_DoctorId'
      AND parent_object_id = OBJECT_ID(N'PrescriptionTemplates'))
BEGIN
    ALTER TABLE [PrescriptionTemplates]
        ADD CONSTRAINT [FK_PrescriptionTemplates_Doctors_DoctorId]
        FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id])
        ON DELETE NO ACTION;
END
");
        }

        /// <summary>
        /// بازگشت به حالت قبل — فقط برای سازگاری ابزار EF؛ در عمل اجرا نمی‌شود.
        /// ردیف‌های NULL به 0 برمی‌گردند و ستون دوباره الزامی می‌شود.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_PrescriptionTemplates_Doctors_DoctorId'
      AND parent_object_id = OBJECT_ID(N'PrescriptionTemplates'))
BEGIN
    ALTER TABLE [PrescriptionTemplates]
        DROP CONSTRAINT [FK_PrescriptionTemplates_Doctors_DoctorId];
END

UPDATE [PrescriptionTemplates] SET [DoctorId] = 0 WHERE [DoctorId] IS NULL;

ALTER TABLE [PrescriptionTemplates] ALTER COLUMN [DoctorId] bigint NOT NULL;

ALTER TABLE [PrescriptionTemplates]
    ADD CONSTRAINT [FK_PrescriptionTemplates_Doctors_DoctorId]
    FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id])
    ON DELETE NO ACTION;
");
        }
    }
}
