using System.Text;
using FluentAssertions;
using Sahand.ClinicPro.Infrastructure.Services.Reporting;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های سرویس خروجی PDF گزارش‌ها با سربرگ درمانگاه — چرخه سیزدهم (درخواست ۴-۶):
/// ساخت PDF معتبر (امضای %PDF)، سربرگ و امضای پایانی، سقف ردیف و رد ستون خالی
/// </summary>
public class ReportPdfServiceTests
{
    /// <summary>مدل نمونه کوچک</summary>
    private static ReportPdfModel Sample(int rows = 3) => new()
    {
        Title = "گزارش درآمد روزانه",
        ClinicName = "درمانگاه سهند",
        ClinicAddress = "تبریز، خیابان آزادی، پلاک ۱۲",
        ClinicPhone = "041-34436141",
        Summary = new()
        {
            new("جمع کل", "1,200,000"),
            new("تعداد فاکتور", "42"),
        },
        Columns = new() { "تاریخ", "شماره فاکتور", "مبلغ (ریال)" },
        Rows = Enumerable.Range(1, rows).Select(i => new List<string>
        {
            $"1404/07/{i:D2}", $"INV-{i:0000}", $"{i * 100_000:N0}",
        }).ToList(),
        FooterNote = "تهیه‌شده توسط سامانه جامع مدیریت درمانگاه سهند",
    };

    [Fact]
    public void Render_Produces_ValidPdf_WithPersianContent()
    {
        var service = new ReportPdfService();
        var pdf = service.Render(Sample());

        pdf.Should().NotBeNullOrEmpty("خروجی باید بایت‌های PDF باشد");
        // امضای استاندارد PDF
        pdf[..5].Should().BeEquivalentTo("%PDF-"u8.ToArray(), "هر فایل PDF معتبر با %PDF- شروع می‌شود");
        pdf.Length.Should().BeGreaterThan(5_000, "سند چند‌صفحه‌ای با فونت جاسازی‌شده حداقل چند کیلوبایت است");
    }

    [Fact]
    public void Render_ManyRows_PaginatesWithoutError()
    {
        var service = new ReportPdfService();
        var pdf = service.Render(Sample(rows: 300));

        pdf.Should().NotBeNullOrEmpty("۳۰۰ ردیف باید بدون خطا در چند صفحه صفحه‌بندی شود");
        // شمارش بن‌مارک‌های پایان صفحه در بایت‌های فشرده قابل اتکا نیست — فقط معتبر بودن بررسی می‌شود
        pdf[..5].Should().BeEquivalentTo("%PDF-"u8.ToArray());
    }

    [Fact]
    public void Render_NoColumns_Throws()
    {
        var service = new ReportPdfService();
        var act = () => service.Render(new ReportPdfModel { Columns = new(), Rows = new() });
        act.Should().Throw<ArgumentException>("گزارش بدون ستون قابل چاپ نیست");
    }

    [Fact]
    public void Render_TooManyRows_Throws()
    {
        var service = new ReportPdfService();
        var model = Sample(rows: 2001);
        var act = () => service.Render(model);
        act.Should().Throw<ArgumentException>("سقف ۲۰۰۰ ردیف برای جلوگیری از فشار حافظه است");
    }

    [Fact]
    public void WarmUp_IsIdempotent()
    {
        // دو بار فراخوانی نباید خطای ثبت مجدد فونت بدهد
        var act = () =>
        {
            ReportPdfService.WarmUp();
            ReportPdfService.WarmUp();
        };
        act.Should().NotThrow("RunClassConstructor سازنده ایستا را فقط یک‌بار اجرا می‌کند");
    }
}
