using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم قلم جدید انبار داروخانه — چرخهٔ سی‌ونه‌م (گزارش مالک)
/// </summary>
/// <remarks>
/// «خرید و انبار و قیمت‌های این لوازم هم باید باشه» — پيش از این چرخه ساخت
/// قلم جدید (دارو یا لوازم مصرفی مثل سرنگ/سرم/گاز استریل) از خود برنامه
/// ممکن نبود. این فرم POST lookups/medications را صدا می‌زند؛ پس از ساخت
/// قلم، کاربر از تب «ورود کالا» بچ/تعداد/قیمت را ثبت می‌کند تا موجودی اولیه
/// بسازد (قلم بدون موجودی در فهرست «موجودی» نمی‌آید چون فهرست فقط بچ‌هاست).
/// </remarks>
public partial class PharmacyItemFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>نام فارسی قلم — الزامی (مثلاً: سرنگ ۵ سی‌سی)</summary>
    [ObservableProperty]
    private string _nameFa = string.Empty;

    /// <summary>نام ژنریک/انگلیسی — الزامی طبق قرارداد جدول؛ برای لوازم مصرفی می‌تواند همان فارسی باشد</summary>
    [ObservableProperty]
    private string _genericName = string.Empty;

    /// <summary>نمایهٔ نوع قلم — ۰ دارو، ۱ لوازم مصرفی (پیش‌فرض: مصرفی — مورد نیاز مالک)</summary>
    [ObservableProperty]
    private int _kindIndex = 1;

    /// <summary>گزینه‌های نوع قلم</summary>
    public string[] KindOptions { get; } = { "💊 دارو", "🧰 لوازم مصرفی (سرنگ/سرم/گاز و…)" };

    /// <summary>فرم/جنس — اختیاری (مثلاً: ست تزریقی)</summary>
    [ObservableProperty]
    private string? _form;

    /// <summary>اندازه/غلظت — اختیاری (مثلاً: ۵cc)</summary>
    [ObservableProperty]
    private string? _strength;

    /// <summary>شرکت سازنده — اختیاری</summary>
    [ObservableProperty]
    private string? _manufacturer;

    /// <summary>بارکد — اختیاری</summary>
    [ObservableProperty]
    private string? _barcode;

    /// <summary>پیام خطای فرم</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ثبت</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>ثبت موفق؟ — پنجره بعد از آن بسته می‌شود</summary>
    public bool Saved { get; private set; }

    /// <summary>سازنده با کلاینت API</summary>
    public PharmacyItemFormViewModel(IApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// نام فارسی را به ژنریک هم کپی می‌کند تا کاربر لوازم مصرفی مجبور به دو
    /// بار تایپ نام نشود — با ویرایش دستی ژنریک، مقدار کاربر حفظ می‌شود.
    /// </summary>
    partial void OnNameFaChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(GenericName))
            GenericName = value;
    }

    /// <summary>ثبت قلم جدید</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NameFa))
        {
            ErrorMessage = "نام فارسی قلم الزامی است";
            return;
        }
        if (string.IsNullOrWhiteSpace(GenericName))
        {
            ErrorMessage = "نام ژنریک/انگلیسی الزامی است — برای لوازم مصرفی می‌توانید همان نام فارسی را بگذارید";
            return;
        }

        IsBusy = true;
        var ok = await _api.PostAsync("lookups/medications", new
        {
            nameFa = NameFa.Trim(),
            genericName = GenericName.Trim(),
            itemKind = KindIndex + 1, // ۱ دارو، ۲ لوازم مصرفی
            form = string.IsNullOrWhiteSpace(Form) ? null : Form.Trim(),
            strength = string.IsNullOrWhiteSpace(Strength) ? null : Strength.Trim(),
            manufacturer = string.IsNullOrWhiteSpace(Manufacturer) ? null : Manufacturer.Trim(),
            barcode = string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim()
        });
        IsBusy = false;

        if (ok)
        {
            Saved = true;
            RequestClose?.Invoke();
        }
        else
        {
            ErrorMessage = _api.LastError ?? "ثبت قلم ناموفق بود";
        }
    }

    /// <summary>انصراف — بستن پنجره بدون ثبت</summary>
    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    /// <summary>درخواست بستن پنجره — ویو مشترک این رویداد را گوش می‌دهد</summary>
    public event Action? RequestClose;
}
