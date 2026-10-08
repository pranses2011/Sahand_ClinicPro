namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// قالب نسخه - نسخه‌های آماده پزشکان برای بیماری‌های پرتکرار
/// </summary>
public class PrescriptionTemplate : AuditableEntity, ISoftDeletable
{
    /// <summary>
    /// شناسه پزشک صاحب قالب — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// «تمپلیت هنوز ذخیره نمی‌شود و خطای "خطای غیر منتظره در سرور رخ داد"
    /// می‌دهد» — ریشهٔ اصلی این خطا کشف شد: این ستون پیش‌تر «long الزامی» بود و
    /// کلید خارجیِ اجباری به جدول پزشکان داشت؛ وقتی کلاینت هنگام ذخیرهٔ تمپلیت
    /// doctorId نمی‌فرستاد (یا پزشکی هنوز انتخاب نشده بود) مقدار پیش‌فرض صفر
    /// درج می‌شد و چون پزشکی با شناسهٔ صفر وجود ندارد، INSERT با نقض کلید خارجی
    /// (FK) شکست می‌خورد → خطای ۵۰۰ «غیر منتظره در سرور». ستون اختیاری
    /// (long?) شد تا تمپلیتِ عمومیِ بدون پزشک هم مجاز باشد و رابطهٔ EF با
    ///IsRequired(false) پیکربندی شد (مهاجرت Cycle23TemplateDoctorNullable).
    /// </summary>
    public long? DoctorId { get; set; }

    /// <summary>نام قالب</summary>
    public string TemplateName { get; set; } = null!;

    /// <summary>دسته قالب</summary>
    public string? Category { get; set; }

    /// <summary>اقلام قالب - JSON</summary>
    public string? Items { get; set; }

    /// <summary>تعداد استفاده</summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>پزشک صاحب قالب</summary>
    public Doctor? Doctor { get; set; }
}
