using System.Reflection;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfDocument = QuestPDF.Fluent.Document;

namespace Sahand.ClinicPro.Infrastructure.Services.Reporting;

/// <summary>مدل داده ورودی سرویس PDF گزارش</summary>
public sealed class ReportPdfModel
{
    /// <summary>عنوان گزارش</summary>
    public string Title { get; init; } = "گزارش";

    /// <summary>نام درمانگاه — سربرگ</summary>
    public string ClinicName { get; init; } = "درمانگاه";

    /// <summary>نشانی درمانگاه</summary>
    public string? ClinicAddress { get; init; }

    /// <summary>تلفن درمانگاه</summary>
    public string? ClinicPhone { get; init; }

    /// <summary>لوگوی درمانگاه — بایت‌های تصویر</summary>
    public byte[]? LogoBytes { get; init; }

    /// <summary>کارت‌های خلاصه — جفت عنوان/مقدار</summary>
    public List<KeyValuePair<string, string>> Summary { get; init; } = new();

    /// <summary>عنوان ستون‌های جدول</summary>
    public List<string> Columns { get; init; } = new();

    /// <summary>ردیف‌های جدول — هر ردیف دقیقاً هم‌اندازه ستون‌ها</summary>
    public List<List<string>> Rows { get; init; } = new();

    /// <summary>یادداشت پایانی — مثل نام تهیه‌کننده</summary>
    public string? FooterNote { get; init; }
}

/// <summary>
/// سرویس خروجی PDF گزارش‌ها با سربرگ درمانگاه — چرخه سیزدهم (درخواست ۴-۶).
/// موتور QuestPDF با مجوز Community؛ فونت فارسی وزیرمتن از منابع جاسازی‌شده
/// اسمبلی خوانده می‌شود تا روی هر سروری بدون نصب فونت کار کند.
/// ساختار صفحه: سربرگ (لوگو + نام + نشانی + تلفن) ← عنوان و تاریخ شمسی ←
/// کارت‌های خلاصه ← جدول RTL رنگ‌بندی‌شده ← پانویس صفحه‌شماری و محل امضا.
/// </summary>
public sealed class ReportPdfService
{
    /// <summary>نام خانواده فونت پس از ثبت</summary>
    public const string PersianFont = "Vazirmatn";

    static ReportPdfService()
    {
        // مجوز Community رایگان — پروژه‌های زیر یک میلیون دلار درآمد سالانه
        QuestPDF.Settings.License = LicenseType.Community;

        // ثبت فونت فارسی وزیرمتن از منابع جاسازی‌شده — یک‌بار برای هر فرایند
        var assembly = typeof(ReportPdfService).Assembly;
        var regular = assembly.GetManifestResourceStream(
            "Sahand.ClinicPro.Infrastructure.Assets.Fonts.Vazirmatn-Regular.ttf");
        var bold = assembly.GetManifestResourceStream(
            "Sahand.ClinicPro.Infrastructure.Assets.Fonts.Vazirmatn-Bold.ttf");
        if (regular is not null) FontManager.RegisterFont(regular);
        if (bold is not null) FontManager.RegisterFont(bold);
    }

    /// <summary>بیدارسازی نوع — اجرای سازنده ایستا و ثبت فونت پیش از نخستین رندر</summary>
    public static void WarmUp()
        => System.Runtime.CompilerServices.RuntimeHelpers
            .RunClassConstructor(typeof(ReportPdfService).TypeHandle);

    /// <summary>رندر مدل گزارش به بایت‌های PDF</summary>
    /// <exception cref="ArgumentException">مدل بدون ستون یا ناسازگار</exception>
    public byte[] Render(ReportPdfModel model)
    {
        if (model.Columns.Count == 0)
            throw new ArgumentException("گزارش بدون ستون قابل چاپ نیست");
        if (model.Rows.Count > 2000)
            throw new ArgumentException("حداکثر ۲۰۰۰ ردیف برای PDF مجاز است — ابتدا محدوده کنید");

        var document = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(14, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontFamily(PersianFont).FontSize(9.5f));
                page.ContentFromRightToLeft();

                // سربرگ هر صفحه
                page.Header().PaddingBottom(6).Column(col =>
                {
                    col.Item().BorderBottom(1.5f).PaddingBottom(6).Row(row =>
                    {
                        // لوگو در سمت راست سربرگ
                        if (model.LogoBytes is { Length: > 0 })
                        {
                            row.ConstantItem(52).Height(52).Image(model.LogoBytes).FitArea();
                        }
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text(model.ClinicName)
                                .Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                            if (!string.IsNullOrWhiteSpace(model.ClinicAddress))
                                c.Item().AlignCenter().Text($"نشانی: {model.ClinicAddress}")
                                    .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(model.ClinicPhone))
                                c.Item().AlignCenter().Text($"تلفن: {model.ClinicPhone}")
                                    .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        });
                        // فضای قرینه سمت چپ
                        if (model.LogoBytes is { Length: > 0 })
                            row.ConstantItem(52);
                    });
                });

                // پانویس: شماره صفحه و نام سامانه
                page.Footer().AlignCenter().PaddingTop(6).Text(t =>
                {
                    t.Span("صفحه ");
                    t.CurrentPageNumber();
                    t.Span(" از ");
                    t.TotalPages();
                    t.Span("  —  سامانه جامع مدیریت درمانگاه سهند");
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    // عنوان گزارش + تاریخ شمسی چاپ
                    col.Item().PaddingBottom(8).Column(c =>
                    {
                        c.Item().Text(model.Title).Bold().FontSize(14);
                        c.Item().Text($"تاریخ چاپ: {Application.Common.Helpers.PersianDateHelper.ToShamsi(DateTime.Now)}")
                            .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    });

                    // کارت‌های خلاصه — چهار کارت در هر ردیف
                    if (model.Summary.Count > 0)
                    {
                        col.Item().PaddingBottom(10).Grid(g =>
                        {
                            g.Columns(4);
                            g.Spacing(5);
                            foreach (var s in model.Summary)
                            {
                                g.Item().Background("#E3F2FD").Border(1).BorderColor("#BBDEFB")
                                    .CornerRadius(4).Padding(6).Column(c =>
                                    {
                                        c.Item().Text(s.Key).FontSize(8f).FontColor(Colors.Grey.Darken1);
                                        c.Item().Text(s.Value).Bold().FontSize(11.5f);
                                    });
                            }
                        });
                    }

                    // جدول نتایج — RTL با ستون‌بندی داینامیک و رنگ یک‌درمیان
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            foreach (var _ in model.Columns) cd.RelativeColumn();
                        });

                        // سرستون‌ها
                        table.Header(h =>
                        {
                            foreach (var c in model.Columns)
                            {
                                h.Cell().Background(Colors.Blue.Darken2).Border(0.5f)
                                    .BorderColor(Colors.Blue.Darken4).Padding(5)
                                    .Text(c).Bold().FontSize(9.5f).FontColor(Colors.White);
                            }
                        });

                        var index = 0;
                        foreach (var r in model.Rows)
                        {
                            var zebra = index % 2 == 1;
                            for (var i = 0; i < model.Columns.Count; i++)
                            {
                                var cell = r.Count > i ? r[i] : "—";
                                table.Cell().Background(zebra ? "#F5F9FE" : Colors.White)
                                    .Border(0.5f).BorderColor("#D0D8E8").Padding(4)
                                    .Text(cell).FontSize(9f);
                            }
                            index++;
                        }
                    });

                    // محل امضا و یادداشت پایانی
                    col.Spacing(18);
                    if (!string.IsNullOrWhiteSpace(model.FooterNote))
                        col.Item().PaddingTop(10).Text(model.FooterNote!)
                            .FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(38);
                            c.Item().AlignCenter().Text("امضای مسئول").FontSize(8.5f);
                        });
                        row.ConstantItem(60);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(38);
                            c.Item().AlignCenter().Text("مهر و امضای درمانگاه").FontSize(8.5f);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
