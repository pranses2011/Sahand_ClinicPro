using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>پیش‌نمایش نمونه محلی فقط برای بوم تأییدشده، بدون ذخیره یا تماس با سرور.</summary>
public static class FreeTemplatePreview
{
    public static string Create(string html, string type, string paper, string? accent, int fontSize, bool showLogo, IReadOnlyCollection<string> allowed)
    {
        if (!FreeTemplateDesign.TryRead(html, allowed, out _)) throw new ArgumentException("بدنه بوم قابل تأیید نیست.");
        var sample = PrintTemplateRenderer.SampleData(type);
        sample.TryAdd("clinic_name", "درمانگاه نمونه");
        sample.TryAdd("clinic_address", "نشانی نمونه");
        sample.TryAdd("clinic_phone", "شماره نمونه");
        sample.TryAdd("print_date", "نمونه");
        var body = "<div style='border:2px solid #b00020;color:#b00020;padding:8px'>پیش‌نویس — داده نمونه، فاقد اعتبار و ذخیره‌نشده</div>" + PrintTemplateRenderer.Render(html, sample);
        return PrintTemplateRenderer.WrapDocument(body, "درمانگاه نمونه", "نشانی نمونه", "شماره نمونه", null,
            accent, fontSize, null, showLogo, paper).Replace("__PRINT_DATE__", "نمونه");
    }
}
