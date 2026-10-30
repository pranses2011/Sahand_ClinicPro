using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Reporting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders.SeedData;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر قالب‌های چاپ پیش‌فرض — چرخه ۱۳ (درخواست ۱-۶)
/// قالب‌های استاندارد که هرکدام پیش‌فرض نوع خود هستند؛ idempotent و بدون رونویسی ویرایش‌های درمانگاه
/// ═══ چرخه سی‌ویکم (گزارش مالک) — رفع دو باگ ریشه‌ای «چندین قالب پیش‌فرض برای همهٔ
/// انواع موجودیت‌های قابل چاپ طراحی شود که در تب طراح قالب دیده نشدند» ═══
/// ۱) این بذر هرگز SaveChangesAsync صدا نمی‌زد؛ موجودیت‌ها فقط در ChangeTracker
///    می‌ماندند و ذخیره‌شان به دوشِ سیدرِ بعدی (AccountingAccountSeeder با Order=۵۰)
///    بود که روی دیتابیس‌های موجود به‌خاطر وجود سرفصل‌ها early-return می‌کند و
///    ذخیره هیچ‌وقت اتفاق نمی‌افتاد — قالب‌های تازه فقط روی دیتابیس خالیِ کامل
///    (که سرفصل هم می‌ساخت) ظاهر می‌شدند. اکنون سیدر خودش ذخیره می‌کند.
/// ۲) همهٔ قالب‌ها با IsDefault=true ساخته می‌شدند در حالی که ایندکس فیلترشدهٔ
///    IX_PrintTemplate_Type_Default فقط «یک» قالب پیش‌فرض برای هر نوع مجاز می‌داند
///    → DbUpdateException روی نصب تازه. اکنون فقط نخستین قالبِ هر نوع (در صورت
///    نبودِ پیش‌فرضِ زندهٔ دیگر) پیش‌فرض می‌شود و بقیه قالب معمولی‌اند.
/// </summary>
public class PrintTemplateSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرا — پس از داده‌های مرجع</summary>
    public int Order => 46;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public PrintTemplateSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>
    /// اجرای بذر — فقط وقتی قالبی «با همین نام استاندارد» وجود ندارد افزوده می‌شود
    /// (چرخه بیست‌وچهارم: شرط از «نوع» به «نام» تغییر کرد تا قالب‌های تازه روی
    /// نصب‌های موجود هم بیایند و ویرایش‌های خود درمانگاه دست‌نخورده بمانند)
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // یک بار در نخستین ارتقا، قبض اصلی به عنوان قالب پیش‌فرض اضافه می‌شود.
        // در اجراهای بعدی به انتخاب کاربر دست نمی‌زنیم. رکورد حذف‌شده نیز
        // نشان‌دهندهٔ اجرای قبلی است تا تغییر انتخاب کاربر دوباره لغو نشود.
        var hasOriginalReceipt = await _context.PrintTemplates.IgnoreQueryFilters()
            .AnyAsync(t => t.Type == PrintTemplateType.ReceptionReceipt
                && t.HtmlBody == BuiltInPrintTemplates.ReceptionReceiptMarker, cancellationToken);
        if (!hasOriginalReceipt)
        {
            var oldDefaults = await _context.PrintTemplates
                .Where(t => !t.IsDeleted && t.Type == PrintTemplateType.ReceptionReceipt && t.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var old in oldDefaults) old.IsDefault = false;
            // ابتدا پیش‌فرض قبلی آزاد شود تا با ایندکس فیلترشده تداخل نکند.
            if (oldDefaults.Count > 0) await _context.SaveChangesAsync(cancellationToken);
            _context.PrintTemplates.Add(new PrintTemplate
            {
                TemplateName = BuiltInPrintTemplates.ReceptionReceiptName,
                Type = PrintTemplateType.ReceptionReceipt,
                Description = "قبض حرارتی اصلی برنامه؛ چاپ با موتور داخلی پیشین انجام می‌شود.",
                HtmlBody = BuiltInPrintTemplates.ReceptionReceiptMarker,
                PaperSize = "Thermal80", Orientation = "Portrait", FontSize = 11,
                AccentColor = "#0078D7", ShowClinicLogo = false, IsActive = true, IsDefault = true
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        // انواعِ دارای پیش‌فرضِ زنده در دیتابیس — قالب تازهٔ همان نوع نباید
        // دوباره پرچم پیش‌فرض بگیرد (ایندکس یکتا اجازه نمی‌دهد).
        // نکته: Cast<int>() روی enum در زمان اجرا InvalidCastException می‌دهد (CA2021)
        // → با Select و تبدیل صریح هر عنصر حل می‌شود
        var defaultTypes = (await _context.PrintTemplates.AsNoTracking()
                .Where(t => !t.IsDeleted && t.IsDefault)
                .Select(t => (int)t.Type)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var addedAny = false;

        // نخستین باری که برای نوعی قالب می‌سازیم، همان قالب پیش‌فرضِ نوع می‌شود؛
        // قالب‌های بعدی همان نوع (مدرن/حرارتی/…) قالب معمولی‌اند
        var newlyDefaultedTypes = new HashSet<int>();

        foreach (var row in PrintTemplateSeedData.Rows)
        {
            var type = (PrintTemplateType)row.Type;
            var exists = await _context.PrintTemplates.AsNoTracking()
                .AnyAsync(t => !t.IsDeleted && t.Type == type && t.TemplateName == row.TemplateName,
                    cancellationToken);
            if (exists) continue; // همین قالب استاندارد قبلاً ثبت شده — دست نمی‌زنیم

            // پیش‌فرض فقط وقتی که نه در دیتابیس پیش‌فرضی برای این نوع هست و
            // نه در همین اجرا قبلاً برایش قالبی پیش‌فرض ساخته‌ایم
            var makeDefault = !defaultTypes.Contains(row.Type) && newlyDefaultedTypes.Add(row.Type);

            _context.PrintTemplates.Add(new PrintTemplate
            {
                TemplateName = row.TemplateName,
                Type = type,
                Description = row.Description,
                HtmlBody = row.HtmlBody,
                PaperSize = row.PaperSize,
                Orientation = row.Orientation,
                FontSize = row.FontSize,
                FontFamily = null,
                ShowClinicLogo = true,
                AccentColor = row.AccentColor,
                IsActive = true,
                IsDefault = makeDefault
            });
            addedAny = true;
        }

        // ═══ اصلاح ریشه‌ای چرخهٔ سی‌ویکم: ذخیرهٔ مستقل — دیگر اتکایی به
        // SaveChangesِ سیدرهای بعدی (که ممکن است early-return کنند) نیست ═══
        if (addedAny)
            await _context.SaveChangesAsync(cancellationToken);
    }
}
