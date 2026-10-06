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
        foreach (var row in PrintTemplateSeedData.Rows)
        {
            var type = (PrintTemplateType)row.Type;
            var exists = await _context.PrintTemplates.AsNoTracking()
                .AnyAsync(t => !t.IsDeleted && t.Type == type, cancellationToken);
            if (exists) continue; // درمانگاه خودش ساخته — دست نمی‌زنیم

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
