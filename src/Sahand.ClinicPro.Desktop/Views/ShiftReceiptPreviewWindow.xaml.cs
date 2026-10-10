using System.Collections.ObjectModel;
using System.Windows;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پیش‌نمایش رسید بستن شیفت — چرخهٔ چهل‌وسوم.
/// مدل نمایشی سبک (رکوردهای قالب‌بندی‌شده با واحد پول نمایشی) جدا از سند
/// چاپی ساخته می‌شود تا پیش‌نمایش و چاپ همیشه یکسان دیده شوند.
/// </summary>
public partial class ShiftReceiptPreviewWindow : Window
{
    /// <summary>مدل نمایشی سطر دوسویهٔ رسید</summary>
    public sealed record ReceiptLine(string Label, string Value);

    /// <summary>داده‌های رسید خام (ریال) — مبنای سند چاپی</summary>
    private readonly ShiftReceipt _receipt;

    /// <summary>سازنده با داده‌های رسید</summary>
    public ShiftReceiptPreviewWindow(ShiftReceipt receipt)
    {
        InitializeComponent();
        _receipt = receipt;

        ClinicName = LocalSettingsStore.Get("Printer.ClinicName") is { Length: > 0 } clinic
            ? clinic : "درمانگاه سهند";
        HeaderLine = $"رسید {Money.ToPersianDigits(receipt.ReceiptNumber)} — صندوق {receipt.RegisterName}" +
                     $" — {receipt.ClosedText} — بستن‌دهنده: {receipt.ClosedByName}";
        DifferenceState = receipt.DifferenceState;
        DifferenceColor = receipt.DifferenceColor;
        FooterLine = $"تعداد تراکنش‌های شیفت: {Money.ToPersianDigits(receipt.PaymentCount.ToString())}" +
                     (string.IsNullOrWhiteSpace(receipt.HandoverNote)
                         ? ""
                         : $" — یادداشت تحویل: {receipt.HandoverNote}");

        SystemLines = new ObservableCollection<ReceiptLine>
        {
            new("نقدی ثبت‌شده", Money.FormatWithUnit(receipt.SystemCash)),
            new("کارتخوان", Money.FormatWithUnit(receipt.SystemCard)),
            new("آنلاین (درگاه)", Money.FormatWithUnit(receipt.SystemOnline)),
            new("جمع سیستم", Money.FormatWithUnit(receipt.SystemTotal)),
        };
        CountedLines = new ObservableCollection<ReceiptLine>
        {
            new("وجه نقد شمرده‌شده", Money.FormatWithUnit(receipt.CountedCash)),
            new("تراکنش‌های کارتخوان", Money.FormatWithUnit(receipt.CountedCard)),
            new("جمع شمارش", Money.FormatWithUnit(receipt.CountedTotal)),
        };
        DifferenceLines = new ObservableCollection<ReceiptLine>
        {
            new("اختلاف نقدی", Money.FormatWithUnit(receipt.CashDifference)),
            new("اختلاف کارتی", Money.FormatWithUnit(receipt.CardDifference)),
            new("اختلاف خالص", Money.FormatWithUnit(receipt.NetDifference)),
        };

        DataContext = this;
    }

    /// <summary>نام درمانگاه — سربرگ</summary>
    public string ClinicName { get; }

    /// <summary>سطر سربرگ — شمارهٔ رسید و صندوق و بستن‌دهنده</summary>
    public string HeaderLine { get; }

    /// <summary>متن وضعیت اختلاف</summary>
    public string DifferenceState { get; }

    /// <summary>رنگ وضعیت اختلاف</summary>
    public string DifferenceColor { get; }

    /// <summary>سطر پانوشت — تراکنش‌ها و یادداشت</summary>
    public string FooterLine { get; }

    /// <summary>سطرهای مبالغ سیستم</summary>
    public ObservableCollection<ReceiptLine> SystemLines { get; }

    /// <summary>سطرهای شمارش فیزیکی</summary>
    public ObservableCollection<ReceiptLine> CountedLines { get; }

    /// <summary>سطرهای اختلاف‌سنجی</summary>
    public ObservableCollection<ReceiptLine> DifferenceLines { get; }

    /// <summary>چاپ رسید — همان سند چاپی ۸۰ میلی‌متری</summary>
    private void OnPrintClicked(object sender, RoutedEventArgs e)
        => ShiftReceiptPrinter.Print(_receipt, ClinicName);

    /// <summary>بستن پنجره</summary>
    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();
}
