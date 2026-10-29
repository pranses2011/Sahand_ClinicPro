using System.Windows;
using System.Windows.Documents;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// چاپ برگهٔ نتیجهٔ آزمایش — چرخهٔ ۶۷ (گزارش مالک):
/// «لیست آزمایشگاه ... امکان چاپ هم داشته باشه».
/// </summary>
/// <remarks>
/// سند جریان راست‌چین A4 با این ساختار:
///   • سربرگ: عنوان درمانگاه (نام از تنظیمات محلی Printer.ClinicName) + «برگهٔ نتیجهٔ آزمایش»
///   • شناسنامهٔ درخواست: شماره، بیمار، پزشک، تاریخ، وضعیت
///   • جدول اقلام: نام آزمایش | نتیجه | واحد | بازهٔ طبیعی | وضعیت (نرمال/غیرعادی) | یادداشت
///   • پانوشت: تاریخ چاپ شمسی + خط امضا
/// همهٔ ارقام فارسی؛ چاپ از دروازهٔ PrintFlow می‌گذرد تا تنظیم «نمایش پیش‌نمایش
/// قبل از چاپ» برای این مسیر هم اعمال شود (قانون هم‌کلاسی‌ها).
/// </remarks>
public static class LabResultPrinter
{
    /// <summary>
    /// ساخت سند و ارسال به چاپ — با احترام به تنظیم پیش‌نمایش (PrintFlow).
    /// </summary>
    /// <param name="detail">جزئیات درخواست از GET lab/orders/{id}</param>
    /// <param name="orderId">شناسهٔ درخواست — برای عنوان کار چاپ</param>
    /// <returns>true اگر چاپ (یا پیش‌نمایش منتهی به چاپ) انجام شد</returns>
    public static bool Print(LabResultViewModel.LabOrderDetail detail, long orderId)
    {
        var doc = BuildResultSheet(detail);
        doc.PageWidth = 793.7;   // A4 — 210 میلی‌متر در ۹۶dpi
        doc.PageHeight = 1122.5; // A4 — 297 میلی‌متر

        var orderNo = detail.OrderNumber ?? orderId.ToString();
        return PrintFlow.Print(doc, $"برگهٔ نتیجهٔ آزمایش {orderNo}",
            PrintJobSettings.PrinterFor("LabResult"), jobKey: "LabResult");
    }

    /// <summary>ساخت سند برگهٔ نتیجه — جدول اقلام با رنگ‌بندی غیرعادی</summary>
    private static FlowDocument BuildResultSheet(LabResultViewModel.LabOrderDetail d)
    {
        var doc = NewDocument();

        // ─── سربرگ ───
        var clinic = LocalSettingsStore.Get("Printer.ClinicName");
        doc.Blocks.Add(Center(string.IsNullOrWhiteSpace(clinic) ? "درمانگاه سهند" : clinic,
            17, FontWeights.Bold));
        doc.Blocks.Add(Center("برگهٔ نتیجهٔ آزمایش", 13, FontWeights.SemiBold, "#455A64"));

        // ─── شناسنامهٔ درخواست — جدول دو ستونهٔ برچسب/مقدار ───
        var info = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brush("#CFD8DC"),
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 14, 0, 12),
        };
        info.Columns.Add(new TableColumn { Width = new GridLength(96) });
        info.Columns.Add(new TableColumn { Width = new GridLength(250) });
        info.Columns.Add(new TableColumn { Width = new GridLength(96) });
        info.Columns.Add(new TableColumn { Width = new GridLength(247) });
        var infoGroup = new TableRowGroup();
        AddInfoRow(infoGroup, "شماره درخواست", Persian(d.OrderNumber ?? "—"),
            "تاریخ درخواست", Persian(d.DateShamsi));
        AddInfoRow(infoGroup, "بیمار", d.PatientName, "پزشک", d.DoctorName);
        AddInfoRow(infoGroup, "وضعیت", StatusTextOf(d.Status), "تاریخ چاپ",
            PersianDateHelper.ToShamsiFa(IranClock.Now));
        info.RowGroups.Add(infoGroup);
        doc.Blocks.Add(info);

        // شرح بالینی — اگر پزشک داده باشد
        if (!string.IsNullOrWhiteSpace(d.ClinicalInfo))
        {
            var clinical = new Paragraph { FontSize = 11, Margin = new Thickness(0, 0, 0, 10) };
            clinical.Inlines.Add(new Run("اطلاعات بالینی: ") { FontWeight = FontWeights.Bold });
            clinical.Inlines.Add(new Run(d.ClinicalInfo) { Foreground = Brush("#455A64") });
            doc.Blocks.Add(clinical);
        }

        // ─── جدول اقلام ───
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brush("#CFD8DC"),
            BorderThickness = new Thickness(0.5),
        };
        // عرض‌ها — جمع ≈ ۷۱۳ (عرض قابل چاپ A4 با حاشیهٔ ۴۰)
        table.Columns.Add(new TableColumn { Width = new GridLength(180) }); // نام آزمایش
        table.Columns.Add(new TableColumn { Width = new GridLength(130) }); // نتیجه
        table.Columns.Add(new TableColumn { Width = new GridLength(70) });  // واحد
        table.Columns.Add(new TableColumn { Width = new GridLength(110) }); // بازهٔ طبیعی
        table.Columns.Add(new TableColumn { Width = new GridLength(95) });  // وضعیت
        table.Columns.Add(new TableColumn { Width = new GridLength(128) }); // یادداشت

        // سرستون
        var head = new TableRowGroup { Background = System.Windows.Media.Brushes.WhiteSmoke };
        var hr = new TableRow();
        foreach (var title in new[] { "نام آزمایش", "نتیجه", "واحد", "بازهٔ طبیعی", "وضعیت", "یادداشت" })
            hr.Cells.Add(HeaderCell(title));
        head.Rows.Add(hr);
        table.RowGroups.Add(head);

        // اقلام — نتیجه‌دار رنگ وضعیت می‌گیرد (غیرعادی = قرمز ملایم)
        var body = new TableRowGroup();
        var items = d.Items ?? new List<LabResultViewModel.LabOrderItemDto>();
        foreach (var item in items)
        {
            var row = new TableRow();
            row.Cells.Add(BodyCell(item.TestName, bold: true));
            row.Cells.Add(BodyCell(string.IsNullOrWhiteSpace(item.Result)
                ? (item.ResultNumeric.HasValue ? Persian(Money.Format(item.ResultNumeric.Value)) : "در انتظار")
                : Persian(item.Result)));
            row.Cells.Add(BodyCell(item.Unit ?? "—"));
            row.Cells.Add(BodyCell(item.NormalRange ?? "—"));
            row.Cells.Add(item.IsAbnormal
                ? BodyCell("غیرعادی ⚠", background: "#FDECEA", foreground: "#B71C1C")
                : (string.IsNullOrWhiteSpace(item.Result) && item.ResultNumeric is null
                    ? BodyCell("—")
                    : BodyCell("نرمال", foreground: "#1B5E20")));
            row.Cells.Add(BodyCell(item.Comment ?? ""));
            body.Rows.Add(row);
        }
        table.RowGroups.Add(body);
        doc.Blocks.Add(table);

        // ─── پانوشت ───
        doc.Blocks.Add(new Paragraph(new Run(
            "نتیجه‌های غیرعادی با علامت هشدار مشخص شده‌اند — در صورت تردید با پزشک مشورت کنید."))
        {
            FontSize = 10,
            Foreground = Brush("#78909C"),
            Margin = new Thickness(0, 12, 0, 0),
        });
        doc.Blocks.Add(new Paragraph(new Run("مهر و امضا: ......................................."))
        {
            FontSize = 11,
            Margin = new Thickness(0, 26, 0, 0),
            Foreground = Brush("#455A64"),
        });

        return doc;
    }

    /// <summary>سند پایه — راست‌چین با فونت چاپ برنامه (وزیرمتن جاسازی‌شده)</summary>
    private static FlowDocument NewDocument() => new()
    {
        FlowDirection = FlowDirection.RightToLeft,
        FontFamily = FontAssets.PrintFont,
        FontSize = 11.5,
        PagePadding = new Thickness(40, 36, 40, 40),
        ColumnWidth = double.PositiveInfinity,
    };

    /// <summary>پاراگراف مرکزچین با رنگ اختیاری</summary>
    private static Paragraph Center(string text, double size, FontWeight weight, string? color = null)
    {
        var p = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2),
        };
        p.Inlines.Add(new Run(text)
        {
            FontSize = size,
            FontWeight = weight,
            Foreground = color is null ? System.Windows.Media.Brushes.Black : Brush(color),
        });
        return p;
    }

    /// <summary>ردیف شناسنامه — دو جفت برچسب/مقدار در یک ردیف</summary>
    private static void AddInfoRow(TableRowGroup group, string l1, string v1, string l2, string v2)
    {
        var row = new TableRow();
        row.Cells.Add(InfoLabelCell(l1));
        row.Cells.Add(InfoValueCell(v1));
        row.Cells.Add(InfoLabelCell(l2));
        row.Cells.Add(InfoValueCell(v2));
        group.Rows.Add(row);
    }

    /// <summary>سلول برچسب شناسنامه — خاکستری کم‌رنگ</summary>
    private static TableCell InfoLabelCell(string text) => new(
        new Paragraph(new Run(text) { FontWeight = FontWeights.SemiBold, FontSize = 10.5 })
        {
            Margin = new Thickness(0),
        })
    {
        Padding = new Thickness(8, 5, 8, 5),
        Background = System.Windows.Media.Brushes.WhiteSmoke,
        BorderBrush = Brush("#E0E0E0"),
        BorderThickness = new Thickness(0.5),
    };

    /// <summary>سلول مقدار شناسنامه</summary>
    private static TableCell InfoValueCell(string text) => new(
        new Paragraph(new Run(text) { FontSize = 11 }) { Margin = new Thickness(0) })
    {
        Padding = new Thickness(8, 5, 8, 5),
        BorderBrush = Brush("#E0E0E0"),
        BorderThickness = new Thickness(0.5),
    };

    /// <summary>سرستون جدول اقلام</summary>
    private static TableCell HeaderCell(string text) => new(
        new Paragraph(new Run(text) { FontWeight = FontWeights.Bold }) { Margin = new Thickness(0) })
    {
        Padding = new Thickness(6, 5, 6, 5),
        BorderBrush = Brush("#B0BEC5"),
        BorderThickness = new Thickness(0.5),
    };

    /// <summary>سلول دادهٔ جدول اقلام — با رنگ زمینه/متن اختیاری</summary>
    private static TableCell BodyCell(string text, bool bold = false,
        string? background = null, string? foreground = null) => new(
        new Paragraph(new Run(text)
        {
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = foreground is null ? System.Windows.Media.Brushes.Black : Brush(foreground),
        })
        {
            Margin = new Thickness(0),
            TextAlignment = TextAlignment.Center,
        })
    {
        Padding = new Thickness(6, 4, 6, 4),
        Background = background is null ? null : Brush(background),
        BorderBrush = Brush("#E0E0E0"),
        BorderThickness = new Thickness(0.5),
    };

    /// <summary>متن فارسی وضعیت درخواست</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "سفارش‌شده",
        2 => "نمونه‌گیری‌شده",
        3 => "در جریان",
        4 => "نتیجه آماده",
        _ => "—",
    };

    /// <summary>بروش رنگ از کد hex — فریز برای عملکرد چاپ</summary>
    private static System.Windows.Media.SolidColorBrush Brush(string hex)
    {
        var brush = (System.Windows.Media.SolidColorBrush?)new System.Windows.Media.BrushConverter().ConvertFromString(hex);
        var result = brush ?? System.Windows.Media.Brushes.Black;
        result.Freeze();
        return result;
    }

    /// <summary>تبدیل ارقام لاتین رشته به فارسی</summary>
    private static string Persian(string input) => Money.ToPersianDigits(input);
}
