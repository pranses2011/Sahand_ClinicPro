using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره فیش حقوقی چاپی — گام R3 + حقوق درصدی چرخه نهم
/// سند FlowDocument استاندارد RTL با سربرگ درمانگاه، جدول مزایا/کسورات
/// (شامل ردیف حقوق درصدی از سود خدمات) و ناحیه امضا؛
/// چاپ مستقیم با PrintDialog (A4 تک‌صفحه‌ای)
/// </summary>
public partial class PayslipWindow : Window
{
    /// <summary>سازنده — همه داده‌های فیش از گرداننده API</summary>
    public PayslipWindow(
        string clinicName, string employeeName, string personnelCode, string nationalCode,
        string periodText, string monthName,
        decimal baseSalary, decimal commissionPercent,
        decimal serviceProfitBase, decimal serviceCommission,
        decimal overtime, decimal bonus,
        decimal insurance, decimal tax, decimal deductions, decimal net)
    {
        InitializeComponent();

        var gross = baseSalary + serviceCommission + overtime + bonus;
        var totalDeductions = insurance + tax + deductions;

        SummaryText.Text = $"فیش حقوقی {employeeName} — دوره {periodText} — خالص: {net:#,0} ریال";

        Viewer.Document = BuildDocument(clinicName, employeeName, personnelCode,
            nationalCode, periodText, monthName, baseSalary, commissionPercent,
            serviceProfitBase, serviceCommission, overtime, bonus,
            gross, insurance, tax, deductions, totalDeductions, net);
    }

    /// <summary>ساخت سند فیش — جدول‌بندی استاندارد فیش ایرانی</summary>
    private static FlowDocument BuildDocument(
        string clinicName, string employeeName, string personnelCode, string nationalCode,
        string periodText, string monthName,
        decimal baseSalary, decimal commissionPercent,
        decimal serviceProfitBase, decimal serviceCommission,
        decimal overtime, decimal bonus, decimal gross,
        decimal insurance, decimal tax, decimal deductions, decimal totalDeductions, decimal net)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(36),
            FontFamily = new FontFamily("pack://application:,,,/Assets/Fonts/#Vazirmatn"),
            FontSize = 11,
            ColumnWidth = double.PositiveInfinity // تک‌ستونه
        };

        // ── سربرگ ──
        var header = new Paragraph(new Run(clinicName)
        {
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.DarkSlateGray
        })
        { TextAlignment = TextAlignment.Center };
        doc.Blocks.Add(header);

        doc.Blocks.Add(new Paragraph(new Run("فیش حقوقی")
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold
        })
        { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 2, 0, 10) });

        // ── شناسنامه فیش ──
        doc.Blocks.Add(InfoTable(employeeName, personnelCode, nationalCode,
            periodText, monthName));

        // ── جدول مزایا و کسورات ──
        doc.Blocks.Add(new Paragraph(new Run("مزایا و کسورات")
        { FontWeight = FontWeights.Bold, FontSize = 12.5 })
        { Margin = new Thickness(0, 10, 0, 4) });

        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.8),
            Margin = new Thickness(0, 0, 0, 4)
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(2.1, GridUnitType.Star) }); // عنوان
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });   // مبلغ
        table.Columns.Add(new TableColumn { Width = new GridLength(2.1, GridUnitType.Star) }); // عنوان کسور
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });   // مبلغ کسور

        // سربرگ جدول
        var head = new TableRowGroup();
        table.RowGroups.Add(head);
        head.Rows.Add(HeaderRow("مزایا", "مبلغ (ریال)", "کسورات", "مبلغ (ریال)"));

        // ردیف‌های زوج — مزایا در ستون‌های راست، کسورات در چپ
        var body = new TableRowGroup();
        table.RowGroups.Add(body);
        body.Rows.Add(PairRow("حقوق پایه", baseSalary, "بیمه سهم کارمند (۷٪)", insurance));
        body.Rows.Add(PairRow(
            $"حقوق درصدی از سود خدمات ({commissionPercent:0.##}٪)",
            serviceCommission, "مالیات حقوق", tax));
        body.Rows.Add(PairRow("اضافه‌کاری", overtime, "سایر کسورات", deductions));
        body.Rows.Add(PairRow("پاداش", bonus, "", null));
        body.Rows.Add(PairRow("جمع مزایا", gross, "جمع کسورات", totalDeductions, bold: true));

        // ردیف خالص — تمام‌عرض
        var netRow = new TableRow { Background = Brushes.Gainsboro };
        var c1 = Cell("خالص پرداختی", bold: true);
        var c2 = Cell($"{net:#,0}", bold: true);
        c1.ColumnSpan = 3;
        netRow.Cells.Add(c1);
        netRow.Cells.Add(c2);
        body.Rows.Add(netRow);

        doc.Blocks.Add(table);

        // ── یادداشت پایه محاسبه ──
        doc.Blocks.Add(new Paragraph(new Run(
            "مبنا: ساعت کار قانونی ۱۹۲ ساعت در ماه؛ اضافه‌کاری ۱/۴ برابر نرخ ساعت عادی؛ " +
            "بیمه سهم کارمند ۷٪؛ پلکان مالیات حقوق مصوب بودجه ۱۴۰۴. " +
            $"حقوق درصدی: {commissionPercent:0.##}٪ از سود خدمات انجام‌شده توسط خودِ این پرسنل در دوره " +
            $"(مبنای سود خدمات: {serviceProfitBase:#,0} ریال) — نه از کل دریافتی درمانگاه. " +
            "این فیش به‌صورت سیستمی صادر شده است.")
        {
            FontSize = 9.5,
            Foreground = Brushes.DimGray
        })
        { Margin = new Thickness(0, 6, 0, 0), TextAlignment = TextAlignment.Right });

        // ── امضاها ──
        var signTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 26, 0, 0) };
        for (var i = 0; i < 3; i++)
            signTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var sg = new TableRowGroup();
        signTable.RowGroups.Add(sg);
        var row = new TableRow();
        row.Cells.Add(SignCell("تنظیم‌کننده — امور مالی"));
        row.Cells.Add(SignCell("تأیید — مدیر درمانگاه"));
        row.Cells.Add(SignCell("امضای گیرنده"));
        sg.Rows.Add(row);
        doc.Blocks.Add(signTable);

        // ── تاریخ صدور ──
        doc.Blocks.Add(new Paragraph(new Run(
            $"تاریخ صدور: {PersianDateHelper.ToShamsiFa(DateTime.Now)}"))
        {
            FontSize = 10,
            Foreground = Brushes.DimGray,
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(0, 18, 0, 0)
        });

        return doc;
    }

    /// <summary>جدول شناسنامه فیش — دو ستونه کلید/مقدار</summary>
    private static Table InfoTable(string employeeName, string personnelCode,
        string nationalCode, string periodText, string monthName)
    {
        var t = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.8)
        };
        for (var i = 0; i < 4; i++)
            t.Columns.Add(new TableColumn
            {
                Width = i % 2 == 0
                    ? new GridLength(1.15, GridUnitType.Star)
                    : new GridLength(1.85, GridUnitType.Star)
            });

        var g = new TableRowGroup();
        t.RowGroups.Add(g);
        g.Rows.Add(TextPairRow("نام و نام خانوادگی", employeeName, "کد پرسنلی", personnelCode));
        g.Rows.Add(TextPairRow("کد ملی", nationalCode, "دوره حقوق", $"{monthName} ({periodText})"));
        return t;
    }

    /// <summary>ردیف چهارسلولی متنی — شناسنامه فیش</summary>
    private static TableRow TextPairRow(string key1, string value1, string key2, string value2)
    {
        var row = new TableRow { Background = Brushes.WhiteSmoke };
        row.Cells.Add(Cell(key1, bold: true));
        row.Cells.Add(Cell(value1));
        row.Cells.Add(Cell(key2, bold: true));
        row.Cells.Add(Cell(value2));
        return row;
    }

    /// <summary>ردیف چهارسلولی مزایا/کسورات</summary>
    private static TableRow PairRow(string earnTitle, decimal earnValue,
        string deductTitle, decimal? deductValue, bool bold = false)
    {
        var row = new TableRow();
        row.Cells.Add(Cell(earnTitle, bold: bold));
        row.Cells.Add(AmountCell(earnValue, bold));
        row.Cells.Add(Cell(deductTitle, bold: bold));
        row.Cells.Add(deductValue.HasValue
            ? AmountCell(deductValue.Value, bold)
            : Cell("—", bold));
        return row;
    }

    /// <summary>ردیف سربرگ جدول</summary>
    private static TableRow HeaderRow(params string[] titles)
    {
        var row = new TableRow { Background = Brushes.SteelBlue };
        foreach (var title in titles)
        {
            var run = new Run(title)
            {
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            row.Cells.Add(new TableCell(new Paragraph(run)
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4, 3, 4, 3)
            })
            { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
        }
        return row;
    }

    /// <summary>سلول متنی با حاشیه</summary>
    private static TableCell Cell(string text, bool bold = false)
        => new(new Paragraph(new Run(text)
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
        })
        { Margin = new Thickness(5, 2.5, 5, 2.5), TextAlignment = TextAlignment.Right })
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(2)
        };

    /// <summary>سلول مبلغی با جداکننده هزارگان و چینش چپ</summary>
    private static TableCell AmountCell(decimal amount, bool bold)
        => new(new Paragraph(new Run(amount.ToString("#,0"))
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
        })
        { Margin = new Thickness(5, 2.5, 5, 2.5), TextAlignment = TextAlignment.Left })
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(2)
        };

    /// <summary>سلول ناحیه امضا</summary>
    private static TableCell SignCell(string title)
        => new(new Paragraph(new Run(title) { FontSize = 10 })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 34, 0, 0)
        })
        { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 0.7) };

    /// <summary>چاپ مستقیم سند — انتخابگر چاپگر سیستم</summary>
    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (Viewer.Document is null) return;

        var dialog = new System.Windows.Controls.PrintDialog();
        if (dialog.ShowDialog() != true) return;

        // سند به‌صورت خودکار به اندازه A4 صفحه‌بندی می‌شود
        Viewer.Document.PageHeight = dialog.PrintableAreaHeight;
        Viewer.Document.PageWidth = dialog.PrintableAreaWidth;

        var paginator = ((IDocumentPaginatorSource)Viewer.Document).DocumentPaginator;
        dialog.PrintDocument(paginator, "فیش حقوقی — سامانه درمانگاه سهند");
    }
}
