using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel ثبت پرداخت — برای یک فاکتور باز
/// </summary>
public partial class PaymentWindowViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long _invoiceId;

    /// <summary>مبلغ باقیمانده — راهنمای کاربر</summary>
    public decimal RemainingAmount { get; }

    /// <summary>مبلغ پرداخت به ریال</summary>
    [ObservableProperty]
    private decimal _amount;

    /// <summary>
    /// نمایه روش پرداخت — ۰ نقدی تا ۴ حساب‌به‌حساب.
    /// ═══ چرخهٔ ۷۰ (گزارش مالک) ═══ با «آخرین روش استفاده‌شده» از تنظیمات
    /// محلی مقدار اولیه می‌گیرد (کلید مشترک با فرم پرداخت پذیرش) و پس از
    /// هر ثبتِ موفق همان‌جا ذخیره می‌شود — باز شدن بعدی با روش قبلی شروع
    /// می‌شود، نه همیشه نقدی.
    /// </summary>
    [ObservableProperty]
    private int _methodIndex = Services.LocalSettingsStore.Get("ui.payment.lastMethod") is { } last
        && int.TryParse(last, out var li) && li is >= 0 and <= 4 ? li : 0;

    /// <summary>شماره پیگیری/مرجع</summary>
    [ObservableProperty]
    private string? _referenceNumber;

    /// <summary>چهار رقم آخر کارت</summary>
    [ObservableProperty]
    private string? _cardNumber;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>سازنده با کلاینت API و شناسه فاکتور و مانده</summary>
    public PaymentWindowViewModel(IApiClient api, long invoiceId, decimal remainingAmount)
    {
        _api = api;
        _invoiceId = invoiceId;
        RemainingAmount = remainingAmount;
        Amount = remainingAmount;
    }

    /// <summary>ثبت پرداخت</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (Amount <= 0)
        {
            ErrorMessage = "مبلغ پرداخت باید بزرگ‌تر از صفر باشد";
            return;
        }

        IsBusy = true;

        // ابتدا شناسهٔ بیمار فاکتور گرفته می‌شود
        // ═══ چرخهٔ ۴۸ — پاکت استاندارد ApiResponse باز شود ═══
        // پاسخ سرور در پاکت {isSuccess, data} است؛ نگاشت مستقیمِ پیشین یعنی
        // PatientId/RemainingAmount همیشه پیش‌فرض (۰) — پرداخت با patientId=0
        // ثبت می‌شد و به هیچ بیماری وصل نمی‌ماند.
        var envelope = await _api.GetAsync<InvoiceDetailEnvelope>($"invoices/{_invoiceId}");
        var detail = envelope?.Data;
        if (detail is null)
        {
            ErrorMessage = _api.LastError ?? "دریافت فاکتور ناموفق بود";
            IsBusy = false;
            return;
        }

        var body = new
        {
            invoiceId = _invoiceId,
            patientId = detail.PatientId,
            amount = Amount,
            method = MethodIndex + 1,
            referenceNumber = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
            cardNumber = string.IsNullOrWhiteSpace(CardNumber) ? null : CardNumber.Trim()
        };

        var ok = await _api.PostAsync("payments", body);
        IsBusy = false;

        if (ok)
        {
            // چرخهٔ ۷۰ — ذخیرهٔ روشِ ثبت‌شده به‌عنوان آخرین روش استفاده‌شده
            Services.LocalSettingsStore.Set("ui.payment.lastMethod", MethodIndex.ToString());
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else ErrorMessage = _api.LastError ?? "ثبت پرداخت ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// پاکت استاندارد پاسخ فاکتور — چرخهٔ ۴۸ (قانون هم‌کلاسی‌ها با باگ پاکت
    /// «ثبت نتیجهٔ آزمایش»): بدون این پوشش، ویژگی‌های فاکتور هرگز بایند نمی‌شدند
    /// و شناسهٔ بیمارِ ارسالی به سرور همیشه ۰ می‌ماند.
    /// </summary>
    public sealed class InvoiceDetailEnvelope
    {
        public bool IsSuccess { get; set; }
        public InvoiceDetail? Data { get; set; }
    }

    public sealed class InvoiceDetail
    {
        public long Id { get; set; }
        public long PatientId { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}