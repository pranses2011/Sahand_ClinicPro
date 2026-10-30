using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجرهٔ عمومی «پیش‌نمایش چاپ» — چرخهٔ ۶۷ (گزارش مالک).
/// پارامترهای سازنده: سند جریان + عنوان + چاپگر مقصد (از تنظیمات).
/// دکمهٔ چاپ از PrintFlow.PrintDirect به «چاپگر مقصد» می‌فرستد (بدون دیالوگ
/// ویندوز) تا رفتار همهٔ چاپ‌های برنامه یکدست بماند؛ پرچم Printed برای
/// فراخواننده گزارش می‌شود (توست «چاپ شد» فقط وقتی واقعاً چاپ شده).
/// </summary>
public partial class PrintPreviewWindow : Window
{
    private readonly FlowDocument _document;
    private readonly string _printTitle;
    private readonly string? _printerName;
    private readonly double? _pageWidth;
    private readonly double? _pageHeight;

    /// <summary>آیا کاربر از این پنجره چاپ کرد؟ (بستن بدون چاپ = false)</summary>
    public bool Printed { get; private set; }

    /// <summary>
    /// سازنده — سند باید PageWidth/PageHeight از قبل گرفته باشد (مسیرهای
    /// چاپ برنامه همهً این کار را می‌کنند؛ قبض نوبت عرض ۸۰ میلی، بقیه A4).
    /// </summary>
    public PrintPreviewWindow(FlowDocument document, string printTitle, string? printerName, bool allowPrint = true,
        double? pageWidth = null, double? pageHeight = null, int copies = 1)
    {
        _document = document;
        _printTitle = printTitle;
        _printerName = printerName;
        _pageWidth = pageWidth;
        _pageHeight = pageHeight;
        InitializeComponent();
        CopiesBox.SelectedIndex = PrintFlow.NormalizeCopies(copies) - 1;
        CopiesBox.Visibility = CopiesLabel.Visibility = allowPrint ? Visibility.Visible : Visibility.Collapsed;

        PrintButton.Visibility = allowPrint ? Visibility.Visible : Visibility.Collapsed;
        // دکمه/میانبر داخلی نمایشگر نباید از تنظیمات چاپگر، اندازه و نسخه
        // عبور کند؛ در نمونهٔ طراح هم هیچ مسیر چاپی باز نمی‌گذاریم.
        DocViewer.CommandBindings.Add(new System.Windows.Input.CommandBinding(
            System.Windows.Input.ApplicationCommands.Print,
            (_, e) => { if (allowPrint) OnPrintClick(this, new RoutedEventArgs()); e.Handled = true; },
            (_, e) => { e.CanExecute = allowPrint; e.Handled = true; }));
        HeaderTitle.Text = "پیش‌نمایش چاپ — " + printTitle;

        // نمایشگر با همان سند مشترک — صفحه‌بندی FlowDocumentPageViewer
        DocViewer.Document = document;

        // زوم آغازین: ۱۰۰٪ — برچسب فارسی با هر تغییر زوم (کلیک دکمه‌ها/کلیدهای
        // میانبر نمایشگر) همگام می‌شود؛ چون FlowDocumentPageViewer رویداد
        // ZoomChanged ندارد، به تغییر خاصیت وابسته از طریق توصیف‌گر گوش می‌دهیم
        UpdateZoomLabel();
        System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
            FlowDocumentPageViewer.ZoomProperty, typeof(FlowDocumentPageViewer))
            .AddValueChanged(DocViewer, (_, _) => UpdateZoomLabel());
    }

    /// <summary>برچسب درصد زوم با ارقام فارسی</summary>
    private void UpdateZoomLabel()
        => ZoomLabel.Text = Money.ToPersianDigits($"{(int)DocViewer.Zoom}٪");

    /// <summary>بزرگ‌نمایی پله‌ای ۱۰٪ — سقف ۳۲۰٪ نمایشگر</summary>
    private void OnZoomIn(object sender, RoutedEventArgs e)
        => DocViewer.Zoom = Math.Min(DocViewer.MaxZoom, DocViewer.Zoom + 10);

    /// <summary>کوچک‌نمایی پله‌ای ۱۰٪ — کف ۴۰٪ نمایشگر</summary>
    private void OnZoomOut(object sender, RoutedEventArgs e)
        => DocViewer.Zoom = Math.Max(DocViewer.MinZoom, DocViewer.Zoom - 10);

    /// <summary>چاپ به چاپگر مقصد تنظیمات — بدون دیالوگ ویندوز (تجربهٔ یکدست)</summary>
    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (CopiesBox.SelectedIndex is < 0 or > 8)
        {
            AppDialog.Error("تعداد نسخه", "تعداد نسخه را بین ۱ تا ۹ انتخاب کنید.");
            return;
        }
        if (PrintFlow.PrintDirect(_document, _printTitle, _printerName, _pageWidth, _pageHeight, CopiesBox.SelectedIndex + 1))
        {
            Printed = true;
            if (PrintFlow.ClosePreviewAfterPrint) Close();
        }
        // شکست چاپ: پیام فارسی درون PrintDirect نشان داده شد؛ پنجره باز می‌ماند
        // تا کاربر بتواند چاپگر را وصل کند و دوباره امتحان کند
    }

    /// <summary>بستن بدون چاپ</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
