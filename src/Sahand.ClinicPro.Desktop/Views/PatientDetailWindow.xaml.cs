using System.Windows;
using System.Windows.Documents;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پرونده بیمار — اتصال ViewModel + چاپ کارت بیمار
/// ═══ چرخهٔ سی‌وهشتم (گزارش مالک: «هیچ‌کدام از دکمه‌های چاپ کار نمی‌کنند») ═══
/// دکمهٔ «چاپ کارت بیمار» نوار بالای پرونده پیش‌تر هیچ فرمانی نداشت (دکمهٔ
/// مرده — فقط ToolTip). اکنون کارت شناسایی بیمار را با سند جریان فارسی
/// راست‌چین می‌سازد (نام/پرونده/کد ملی/تماس/بیمه/خون/نشانی + تاریخ چاپ شمسی)
/// و مستقیم به چاپگر ویندوز می‌فرستد.
/// </summary>
public partial class PatientDetailWindow : Window
{
    /// <summary>سازنده با ViewModel پرونده</summary>
    public PatientDetailWindow(PatientDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// چاپ کارت بیمار — سند جریان راست‌چین با قاب کارت گوشه‌گرد و فیلدهای
    /// هویتی؛ پس از تأیید چاپگر مستقیم چاپ می‌شود.
    /// </summary>
    private void OnPrintCardClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PatientDetailViewModel vm) return;
        if (string.IsNullOrWhiteSpace(vm.FullName))
        {
            AppDialog.Warn("پرونده خالی", "مشخصات بیمار هنوز بارگذاری نشده است — کمی صبر کنید و دوباره بگیرید.");
            return;
        }

        // ═══ چرخهٔ ۶۷ — دروازهٔ یکپارچهٔ چاپ (پیش‌نمایش اختیاری) ═══
        // پیش‌تر این مسیر ابتدا دیالوگ چاپ ویندوز را باز می‌کرد؛ اکنون اگر
        // تنظیم «نمایش پیش‌نمایش قبل از چاپ» روشن باشد، پنجرهٔ پیش‌نمایش فارسی
        // باز می‌شود و چاپ از همان‌جا (به چاپگر پیش‌فرض تنظیمات) انجام می‌شود؛
        // با خاموشیِ تنظیم، چاپ مستقیم است.
        var app = System.Windows.Application.Current;
        var font = app?.TryFindResource("App.FontFamily") as System.Windows.Media.FontFamily
                   ?? new System.Windows.Media.FontFamily("Tahoma");

        var doc = new FlowDocument
        {
            FlowDirection = System.Windows.FlowDirection.RightToLeft,
            FontFamily = font,
            FontSize = 12,
            PagePadding = new System.Windows.Thickness(48, 44, 48, 44),
        };

        // سربرگ کارت — عنوان و تاریخ چاپ شمسی
        doc.Blocks.Add(new Paragraph(new Run("کارت بیمار — درمانگاه سهند"))
        {
            FontSize = 16,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DarkSlateGray,
            Margin = new System.Windows.Thickness(0, 0, 0, 2),
        });
        doc.Blocks.Add(new Paragraph(new Run(
            $"تاریخ چاپ: {PersianDateHelper.ToShamsiFa(IranClock.Now)}"))
        {
            FontSize = 10.5,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new System.Windows.Thickness(0, 0, 0, 14),
        });

        // جدول دوسطونی مشخصات — برچسب‌ها درشت/خاکستری و مقادیر معمولی
        // (چرخهٔ ۶۷: عرض از «صفحهٔ قابل چاپِ دیالوگ» به عرض استاندارد A4 تغییر کرد —
        //  دیالوگ چاپ دیگر باز نمی‌شود؛ سند از دروازهٔ PrintFlow می‌گذرد)
        var table = new Table { CellSpacing = 0 };
        var colWidth = Math.Floor((713.7 - 4) / 2); // ۷۹۳٫۷ (A4) − ۸۰ حاشیهٔ افقی
        table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(colWidth, System.Windows.GridUnitType.Pixel) });
        table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(colWidth, System.Windows.GridUnitType.Pixel) });

        var group = new TableRowGroup();
        var fields = new (string Label, string Value)[]
        {
            ("نام و نام خانوادگی", vm.FullName),
            ("شماره پرونده", vm.MedicalRecordNo ?? "—"),
            ("کد ملی", vm.NationalCode ?? "—"),
            ("موبایل", vm.Mobile ?? "—"),
            ("تلفن", vm.Phone ?? "—"),
            ("بیمه", $"{vm.InsuranceName} ({vm.InsuranceNumber})"),
            ("گروه خونی", vm.BloodType ?? "نامشخص"),
            ("سن / جنسیت", $"{vm.AgeText} — {vm.GenderText}"),
            ("شهر", vm.City ?? "—"),
            ("نشانی", vm.Address ?? "—"),
        };
        for (var i = 0; i < fields.Length; i += 2)
        {
            var row = new TableRow();
            // ستون راست (RTL: نخستین سلول) — فیلد زوج؛ ستون چپ — فیلد فرد
            row.Cells.Add(MakeFieldCell(fields[i].Label, fields[i].Value));
            row.Cells.Add(i + 1 < fields.Length
                ? MakeFieldCell(fields[i + 1].Label, fields[i + 1].Value)
                : new TableCell(new Paragraph(new Run("")) { Margin = new System.Windows.Thickness(0) })
                {
                    Padding = new System.Windows.Thickness(8, 6, 8, 6),
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new System.Windows.Thickness(0.5),
                });
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);
        doc.Blocks.Add(table);

        // یادداشت پایانی — جهت مراجعهٔ بعدی (خط خالی دستی)
        doc.Blocks.Add(new Paragraph(new Run(
            "یادداشت مراجعهٔ بعد: ..................................................................................."))
        {
            FontSize = 11.5,
            Margin = new System.Windows.Thickness(0, 22, 0, 0),
            Foreground = System.Windows.Media.Brushes.DarkSlateGray,
        });

        // ═══ چرخهٔ ۶۷ — پایان مسیر یکپارچه: سند به دروازهٔ PrintFlow سپرده می‌شود ═══
        doc.PageWidth = 793.7;   // A4 — 210 میلی‌متر در ۹۶dpi
        doc.PageHeight = 1122.5; // A4 — 297 میلی‌متر
        Services.PrintFlow.Print(doc, $"کارت بیمار: {vm.FullName}",
            Services.PrintJobSettings.PrinterFor("PatientCard"));
    }

    /// <summary>سلول «برچسب + مقدار» با قاب خاکستری — یک سلول از جدول کارت</summary>
    private static TableCell MakeFieldCell(string label, string value)
    {
        var paragraph = new Paragraph { Margin = new System.Windows.Thickness(0) };
        paragraph.Inlines.Add(new Run($"{label}: ")
        {
            FontWeight = System.Windows.FontWeights.Bold,
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
        });
        paragraph.Inlines.Add(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value)
        {
            FontSize = 12.5,
            FontWeight = System.Windows.FontWeights.SemiBold,
        });
        return new TableCell(paragraph)
        {
            Padding = new System.Windows.Thickness(8, 6, 8, 6),
            BorderBrush = System.Windows.Media.Brushes.LightGray,
            BorderThickness = new System.Windows.Thickness(0.5),
        };
    }
}
