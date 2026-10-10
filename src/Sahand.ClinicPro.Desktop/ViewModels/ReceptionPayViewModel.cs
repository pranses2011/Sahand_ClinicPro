using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// نتیجهٔ پنجرهٔ پرداخت پذیرش — روش + مرجع + چهار رقم کارت
/// </summary>
/// <param name="Method">روش پرداخت — ۱ نقدی، ۲ کارتخوان</param>
/// <param name="ReferenceNumber">شماره پیگیری/مرجع کارتخوان — فقط برای کارتی</param>
/// <param name="CardNumber">چهار رقم آخر کارت — فقط برای کارتی</param>
public record ReceptionPaymentInfo(int Method, string? ReferenceNumber, string? CardNumber);

/// <summary>
/// ViewModel پنجرهٔ پرداخت پذیرش — چرخهٔ سی‌ونه‌م (گزارش مالک)
/// </summary>
/// <remarks>
/// «در پذیرش فقط پرداخت نقدی وجود داره و کارت خوان پیاده سازی نشده. اینو هم
/// به بهترین شکل پیاده سازی بکن» — پیش از این، دکمهٔ «پذیرش و پرداخت نقدی»
/// بدون پرسیدن روش، پرداخت نقدی ثبت می‌کرد. اکنون پیش از ثبت زنجیره، این
/// پنجره باز می‌شود: مبلغ قابل پرداخت (خدمت + لوازم مصرفی) + انتخاب روش
/// (نقدی/کارتخوان) + برای کارتی، شمارهٔ پیگیری و چهار رقم آخر کارت (همان
/// الگوی فرم «ثبت پرداخت» ماژول مالی) — خروجی به زنجیرهٔ پذیرش تزریق می‌شود.
/// </remarks>
public partial class ReceptionPayViewModel : ObservableObject
{
    /// <summary>مبلغ قابل پرداخت بیمار (خدمت + لوازم مصرفی) — قالب‌بندی‌شده</summary>
    [ObservableProperty]
    private string _payableText = string.Empty;

    /// <summary>نمایهٔ روش پرداخت — ۰ نقدی، ۱ کارتخوان</summary>
    [ObservableProperty]
    private int _methodIndex;

    /// <summary>آیا روش پرداخت کارتخوان است؟ — نمایان‌سازی فیلدهای مرجع/کارت</summary>
    public bool IsCard => MethodIndex == 1;

    /// <summary>شماره پیگیری/مرجع تراکنش کارتخوان</summary>
    [ObservableProperty]
    private string? _referenceNumber;

    /// <summary>چهار رقم آخر کارت بانکی</summary>
    [ObservableProperty]
    private string? _cardNumber;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>نتیجهٔ تأیید — پس از Confirm معتبر است</summary>
    public ReceptionPaymentInfo? Result { get; private set; }

    /// <summary>گزینه‌های روش پرداخت</summary>
    public string[] MethodOptions { get; } = { "💵 نقدی", "💳 کارتخوان (POS)" };

    /// <summary>سازنده با مبلغ قالب‌بندی‌شدهٔ قابل پرداخت</summary>
    /// <param name="payableText">مثلاً «۲۵۰٬۰۰۰»</param>
    public ReceptionPayViewModel(string payableText)
    {
        _payableText = payableText;
    }

    /// <summary>بازخورد تغییر روش → نوسازی نمایش فیلدهای کارتی</summary>
    partial void OnMethodIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsCard));
        ErrorMessage = string.Empty;
    }

    /// <summary>تأیید پرداخت — اعتبارسنجی سبک و ساخت نتیجه</summary>
    [RelayCommand]
    private void Confirm()
    {
        // برای کارتخوان، شمارهٔ پیگیری توصیه‌شده ولی الزامی نیست (کاریو کارتخوان
        // ممکن است در لحظه چاپ ندهد)؛ چهار رقم کارت هم اختیاری است — درست مثل
        // فرم ثبت پرداخت ماژول مالی؛ تنها قاعده: اگر کارت ارسال شود باید ۴ رقم باشد
        if (IsCard && !string.IsNullOrWhiteSpace(CardNumber) && CardNumber.Trim().Length != 4)
        {
            ErrorMessage = "چهار رقم آخر کارت باید دقیقاً ۴ رقم باشد";
            return;
        }

        Result = new ReceptionPaymentInfo(
            IsCard ? 2 : 1,
            string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
            IsCard && !string.IsNullOrWhiteSpace(CardNumber) ? CardNumber.Trim() : null);

        RequestClose?.Invoke();
    }

    /// <summary>انصراف — بدون نتیجه</summary>
    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    /// <summary>درخواست بستن پنجره — ویو گوش می‌دهد</summary>
    public event Action? RequestClose;
}
