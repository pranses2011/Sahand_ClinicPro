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
    private PendingPayment? _pendingPayment;
    private readonly IPaymentRecoveryStore? _recoveryStore;
    private readonly string _recoveryServer;
    private long? CurrentBranch => (_api as ApiClient)?.BranchId;
    private bool RecoveryContextMatches => _pendingPayment is not null &&
        _pendingPayment.Server == _recoveryServer && _pendingPayment.BranchId == CurrentBranch &&
        (_api is not ApiClient client || client.PaymentRecoveryServer == _recoveryServer);
    public bool CanRetryPayment => !IsBusy && IsPaymentOutcomeUnknown && RecoveryContextMatches;

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
    public PaymentWindowViewModel(IApiClient api, long invoiceId, decimal remainingAmount,
        IPaymentRecoveryStore? recoveryStore = null)
    {
        _api = api;
        _invoiceId = invoiceId;
        _recoveryStore = recoveryStore ?? (api is ApiClient ? new PaymentRecoveryStore() : null);
        _recoveryServer = api is ApiClient client ? client.PaymentRecoveryServer : "test-api";
        RemainingAmount = remainingAmount;
        Amount = remainingAmount;
        if (_recoveryStore is null) return;
        try
        {
            _pendingPayment = _recoveryStore.Load(_recoveryServer, invoiceId);
            if (_pendingPayment is null) return;
            Amount = _pendingPayment.Amount;
            MethodIndex = _pendingPayment.Method - 1;
            ReferenceNumber = _pendingPayment.ReferenceNumber;
            CardNumber = _pendingPayment.CardNumber;
            IsPaymentOutcomeUnknown = true;
            ErrorMessage = RecoveryContextMatches
                ? "درخواست پرداخت حل‌نشده بازیابی شد. پس از بررسی وجه دریافتی، می‌توانید همان درخواست را دوباره ارسال کنید."
                : "درخواست پرداخت حل‌نشده مربوط به شعبه دیگری است؛ ابتدا به شعبه اصلی برگردید و سوابق را بررسی کنید.";
        }
        catch (Exception)
        {
            IsPaymentOutcomeUnknown = true;
            ErrorMessage = "بازیابی محلی پرداخت قابل خواندن نیست؛ پرداخت جدید مسدود است. سوابق را بررسی و با پشتیبانی تماس بگیرید.";
        }
    }

    /// <summary>ثبت پرداخت</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy || IsPaymentOutcomeUnknown) return;
        ErrorMessage = string.Empty;
        if (Amount <= 0)
        { ErrorMessage = "مبلغ پرداخت باید بزرگ‌تر از صفر باشد"; return; }
        if (Amount > RemainingAmount || Amount > 999999999999999999m || decimal.Truncate(Amount) != Amount ||
            MethodIndex is < 0 or > 4 || ReferenceNumber?.Trim().Length > 50 || CardNumber?.Trim().Length > 20)
        { ErrorMessage = "مبلغ صحیح ریالی، روش یا اطلاعات پرداخت معتبر نیست."; return; }
        var branch = CurrentBranch;
        if (_api is ApiClient serverClient && serverClient.PaymentRecoveryServer != _recoveryServer)
        { ErrorMessage = "سرور تغییر کرده است؛ پنجره را دوباره باز کنید."; return; }

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
            if (CurrentBranch != branch || (_api is ApiClient activeClient && activeClient.PaymentRecoveryServer != _recoveryServer))
            { ErrorMessage = "سرور یا شعبه تغییر کرد؛ پرداخت ارسال نشد."; return; }
            var body = new PendingPayment(_invoiceId, detail.PatientId, Amount, MethodIndex + 1,
                string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
                string.IsNullOrWhiteSpace(CardNumber) ? null : CardNumber.Trim(), Guid.NewGuid(), _recoveryServer, branch);
            try { _recoveryStore?.Reserve(body); }
            catch (Exception)
            {
                // ممکن است پنجره دیگری رزرو کرده یا فایل نیمه‌کاره مانده باشد؛ ارسال تازه ممنوع است.
                try { _pendingPayment = _recoveryStore?.Load(_recoveryServer, _invoiceId); } catch (Exception) { _pendingPayment = null; }
                IsPaymentOutcomeUnknown = true;
                ErrorMessage = "رزرو ایمن پرداخت ناموفق بود و درخواست جدید ارسال نشد؛ سوابق و بازیابی محلی را بررسی کنید.";
                return;
            }
            _pendingPayment = body;
            if (!RecoveryContextMatches)
            {
                IsPaymentOutcomeUnknown = true;
                ErrorMessage = "شعبه یا سرور تغییر کرد؛ درخواست ارسال نشد و بازیابی محلی برای بررسی حفظ شد.";
                return;
            }
            sent = true;
            var ok = await _api.PostAsync("payments/idempotent", body.ToRequest());
            if (!ok)
            {
                IsPaymentOutcomeUnknown = true;
                ErrorMessage = "نتیجه پرداخت نامعلوم است؛ می‌توانید همین درخواست را دوباره بررسی کنید یا پیش از پرداخت جدید سوابق را بررسی کنید. " + _api.LastError;
                return;
            }
            if (!ClearRecovery()) return;
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
        if (!RecoveryContextMatches)
        { ErrorMessage = "سرور یا شعبه تغییر کرده است؛ درخواست را در محل اصلی بررسی کنید."; return; }
        IsBusy = true;
        try
        {
            var ok = await _api.PostAsync("payments/idempotent", _pendingPayment!.ToRequest());
            if (!ok)
            { ErrorMessage = "نتیجه پرداخت همچنان نامعلوم است؛ قبل از پرداخت جدید سوابق را بررسی کنید. " + _api.LastError; return; }
            if (!ClearRecovery()) return;
            Services.LocalSettingsStore.Set("ui.payment.lastMethod", MethodIndex.ToString());
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception) { ErrorMessage = "نتیجه پرداخت همچنان نامعلوم است؛ قبل از پرداخت جدید سوابق را بررسی کنید."; }
        finally { IsBusy = false; }
    }

    private bool ClearRecovery()
    {
        try
        {
            if (_pendingPayment is not null) _recoveryStore?.Clear(_pendingPayment);
            _pendingPayment = null;
            return true;
        }
        catch (Exception)
        {
            IsPaymentOutcomeUnknown = true;
            ErrorMessage = "پرداخت تأیید شد اما فایل بازیابی پاک نشد؛ برای بررسی دوباره همان درخواست یا رفع اشکال با پشتیبانی اقدام کنید.";
            return false;
        }
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