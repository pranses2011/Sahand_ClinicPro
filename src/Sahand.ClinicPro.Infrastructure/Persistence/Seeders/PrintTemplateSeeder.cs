using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Reporting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders.SeedData;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر قالب‌های چاپ پیش‌فرض — چرخه ۱۳ (درخواست ۱-۶)
/// ۹ قالب استاندارد که هرکدام پیش‌فرض نوع خود هستند؛ idempotent و بدون رونویسی ویرایش‌های درمانگاه
/// </summary>
public class PrintTemplateSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرا — پس از داده‌های مرجع</summary>
    public int Order => 46;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public PrintTemplateSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>اجرای بذر — فقط وقتی برای نوعی قالبی وجود ندارد</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) ═══ پیش‌تر شرط «برای نوع قالبی
        // هست؟» بود؛ یعنی روی دیتابیس‌های موجود طرح‌های تازهٔ چرخهٔ ۲۴ هرگز
        // اضافه نمی‌شدند و هر نوع فقط یک قالب اولیه داشت. اکنون شرط به «قالبی
        // با همین نام هست؟» تغییر کرد: قالب‌های استاندارد/مدرن/حرارتی جدید روی
        // نصب‌های موجود هم idempotent افزوده می‌شوند و ویرایش‌های خود درمانگاه
        // (نام‌گذاری‌شدهٔ متفاوت) دست‌نخورده می‌مانند.
        foreach (var row in PrintTemplateSeedData.Rows)
        {
            var type = (PrintTemplateType)row.Type;
            var exists = await _context.PrintTemplates.AsNoTracking()
                .AnyAsync(t => !t.IsDeleted && t.Type == type && t.TemplateName == row.TemplateName,
                    cancellationToken);
            if (exists) continue; // همین قالب استاندارد قبلاً ثبت شده — دست نمی‌زنیم

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
                IsDefault = true
            });
        }
    }
}
