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

    /// <summary>نمایه روش پرداخت</summary>
    [ObservableProperty]
    private int _methodIndex;

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

        // ابتدا شناسه بیمار فاکتور گرفته می‌شود
        var detail = await _api.GetAsync<InvoiceDetail>($"invoices/{_invoiceId}");
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

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ثبت پرداخت ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>جزئیات فاکتور — فقط برای شناسه بیمار</summary>
    public sealed class InvoiceDetail
    {
        public long Id { get; set; }
        public long PatientId { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}