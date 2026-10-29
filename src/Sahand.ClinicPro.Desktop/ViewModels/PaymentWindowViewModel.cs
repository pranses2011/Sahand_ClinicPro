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
    private object? _pendingPayment;
    public bool CanRetryPayment => !IsBusy && IsPaymentOutcomeUnknown && _pendingPayment is not null;

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
    [ObservableProperty] private bool _isPaymentOutcomeUnknown;
    public bool CanEditPayment => !IsBusy && !IsPaymentOutcomeUnknown;
    partial void OnIsBusyChanged(bool value) { OnPropertyChanged(nameof(CanEditPayment)); OnPropertyChanged(nameof(CanRetryPayment)); }
    partial void OnIsPaymentOutcomeUnknownChanged(bool value) { OnPropertyChanged(nameof(CanEditPayment)); OnPropertyChanged(nameof(CanRetryPayment)); }

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
        if (IsBusy || IsPaymentOutcomeUnknown) return;
        ErrorMessage = string.Empty;
        if (Amount <= 0)
        { ErrorMessage = "مبلغ پرداخت باید بزرگ‌تر از صفر باشد"; return; }
        if (Amount > RemainingAmount || MethodIndex is < 0 or > 4)
        { ErrorMessage = "مبلغ از مانده بیشتر است یا روش پرداخت معتبر نیست."; return; }

        IsBusy = true;
        var sent = false;
        try
        {
            var envelope = await _api.GetAsync<InvoiceDetailEnvelope>($"invoices/{_invoiceId}");
            var detail = envelope?.Data;
            if (detail is null || detail.Id != _invoiceId || detail.PatientId <= 0)
            { ErrorMessage = _api.LastError ?? "دریافت هویت فاکتور ناموفق بود"; return; }
            if (detail.RemainingAmount != RemainingAmount || Amount > detail.RemainingAmount)
            { ErrorMessage = "مانده فاکتور تغییر کرده است؛ پنجره را ببندید و فاکتور را تازه‌سازی کنید."; return; }
            var body = new
            {
                invoiceId = _invoiceId, patientId = detail.PatientId, amount = Amount, method = MethodIndex + 1,
                requestId = Guid.NewGuid(),
                referenceNumber = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
                cardNumber = string.IsNullOrWhiteSpace(CardNumber) ? null : CardNumber.Trim()
            };
            _pendingPayment = body;
            sent = true;
            var ok = await _api.PostAsync("payments/idempotent", body);
            if (!ok)
            {
                IsPaymentOutcomeUnknown = true;
                ErrorMessage = "نتیجه پرداخت نامعلوم است؛ می‌توانید همین درخواست را دوباره بررسی کنید یا پیش از پرداخت جدید سوابق را بررسی کنید. " + _api.LastError;
                return;
            }
            Services.LocalSettingsStore.Set("ui.payment.lastMethod", MethodIndex.ToString());
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            if (sent)
            {
                IsPaymentOutcomeUnknown = true;
                ErrorMessage = "نتیجه پرداخت نامعلوم است؛ می‌توانید همین درخواست را دوباره بررسی کنید یا پیش از پرداخت جدید سوابق را بررسی کنید.";
            }
            else ErrorMessage = "دریافت فاکتور ناموفق بود؛ پرداخت ارسال نشد.";
        }
        finally { IsBusy = false; }
    }

    /// <summary>تکرار دقیق همان درخواست و شناسه، بدون ارسال پرداخت جدید.</summary>
    [RelayCommand]
    private async Task RetryPaymentAsync()
    {
        if (!CanRetryPayment) return;
        IsBusy = true;
        try
        {
            var ok = await _api.PostAsync("payments/idempotent", _pendingPayment);
            if (!ok)
            { ErrorMessage = "نتیجه پرداخت همچنان نامعلوم است؛ قبل از پرداخت جدید سوابق را بررسی کنید. " + _api.LastError; return; }
            Services.LocalSettingsStore.Set("ui.payment.lastMethod", MethodIndex.ToString());
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception) { ErrorMessage = "نتیجه پرداخت همچنان نامعلوم است؛ قبل از پرداخت جدید سوابق را بررسی کنید."; }
        finally { IsBusy = false; }
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() { if (!IsBusy) CloseRequested?.Invoke(this, EventArgs.Empty); }

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