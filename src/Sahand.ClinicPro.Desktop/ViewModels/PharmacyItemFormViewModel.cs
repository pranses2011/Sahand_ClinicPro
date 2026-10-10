using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم قلم جدید انبار — چرخهٔ سی‌ونه‌م (گزارش مالک)
/// </summary>
/// <remarks>
/// «خرید و انبار و قیمت‌های این لوازم هم باید باشه» — پیش از چرخهٔ ۳۹ ساخت
/// قلم جدید (دارو یا لوازم مصرفی مثل سرنگ/سرم/گاز استریل) از خود برنامه
/// ممکن نبود. این فرم POST consumables/items را صدا می‌زند (مسیر مستقل
/// مصرفی‌ها یا داروخانه — هر دو به همان فرمان می‌رسند)؛ پس از ساخت
/// قلم، کاربر از فرم «ورود کالا» بچ/تعداد/قیمت را ثبت می‌کند تا موجودی
/// اولیه بسازد (قلم بدون موجودی با مقدار صفر در فهرست انبار دیده می‌شود
/// — چرخهٔ ۴۳).
///
/// ═══ چرخهٔ چهل‌وسوم (گزارش مالک) ═══
/// «چون اقلام مصرفی دیگه از داروخانه مستقل شده، در افزودن قلم جدید و
/// ورود کالا نباید نوع قلم وجود داشته باشه» — کمبوی «نوع قلم» حذف شد؛
/// نوع از «بافت فراخواننده» می‌آید: صفحهٔ لوازم مصرفی همیشه «مصرفی»
/// می‌سازد و داروخانه همیشه «دارو» (سمت سرور هم با ExpectedItemKind قفل
/// شده). نام ژنریک برای مصرفی‌ها خودکار و «کامل» از نام فارسی کپی می‌شود
/// (پیش‌تر باگ نیمه‌کاره فقط حرف اول را کپی می‌کرد و ریشهٔ «قلم تکراری
/// کاذب» چرخهٔ ۴۳ بود)؛ کاربر می‌تواند دستی آن را ویرایش کند و از آن
/// پس دیگر خودکار بازنویسی نمی‌شود.
/// </remarks>
public partial class PharmacyItemFormViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly bool _isConsumableContext;

    /// <summary>
    /// سازنده با کلاینت API و بافت فراخواننده
    /// </summary>
    /// <param name="api">کلاینت HTTP برنامه</param>
    /// <param name="isConsumableContext">
    /// true = فرم از صفحهٔ «لوازم مصرفی» باز شده (قلم همیشه مصرفی ثبت می‌شود)؛
    /// false = از داروخانه (قلم همیشه دارو). نوع دیگر از کاربر پرسیده نمی‌شود —
    /// گزارش مالک چرخهٔ ۴۳: «نباید نوع قلم وجود داشته باشه».
    /// </param>
    public PharmacyItemFormViewModel(IApiClient api, bool isConsumableContext = true)
    {
        _api = api;
        _isConsumableContext = isConsumableContext;
    }

    /// <summary>عنوان بافت‌دار فرم — سربرگ پنجره</summary>
    public string FormTitle => _isConsumableContext
        ? "افزودن قلم مصرفی جدید"
        : "افزودن داروی جدید به انبار";

    /// <summary>راهنمای بافت‌دار بالای فرم</summary>
    public string FormHint => _isConsumableContext
        ? "نخست قلم مصرفی را بسازید؛ سپس از همان صفحه «ورود کالا» بچ، تعداد و قیمت خرید/فروش را ثبت کنید."
        : "نخست دارو را بسازید؛ سپس از تب «موجودی» داروخانه با «ورود کالا» بچ و قیمت ثبت کنید.";

    /// <summary>نام فارسی قلم — الزامی (مثلاً: سرنگ ۵ سی‌سی)</summary>
    [ObservableProperty]
    private string _nameFa = string.Empty;

    /// <summary>
    /// نام ژنریک/انگلیسی — الزامی طبق قرارداد جدول؛ برای لوازم مصرفی می‌تواند
    /// همان فارسی باشد. تا وقتی کاربر دستی آن را تغییر نداده، همگام با نام
    /// فارسی خودکار پر می‌شود.
    /// </summary>
    [ObservableProperty]
    private string _genericName = string.Empty;

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

    // ═══ چرخهٔ ۴۳ — رونوشت خودکار «کامل و هوشمند» نام ژنریک ═══
    // ریشهٔ تکراری کاذب اقلام: پیش‌تر «اگر ژنریک خالی بود» کپی می‌شد که یعنی
    // فقط پس از نخستین نویسهٔ نام فارسی کپی انجام می‌شد و بعدش دیگر هرگز
    // (ژنریک دیگر خالی نبود!) — نتیجه: قلم «سرنگ ۵ سی سی» با ژنریکِ تک‌حرفیِ
    // «س» ثبت می‌شد و قلم بعدیِ هم‌حرفِ آغازین، در گارد تکراریِ نام ژنریک
    // گیر می‌افتاد. اکنون تا وقتی کاربر «خودش» ژنریک را دست‌نخورده گذاشته،
    // همگام کامل با نام فارسی بازنویسی می‌شود؛ با نخستین ویرایش دستی،
    // مقدار کاربر برای همیشه محترم شمرده می‌شود.
    /// <summary>آیا نام ژنریک هنوز «خودکار» است و کاربر آن را ویرایش نکرده؟</summary>
    private bool _genericNameIsAuto = true;

    /// <summary>
    /// رونوشت خودکار کامل نام ژنریک از نام فارسی — فقط تا پیش از ویرایش دستی
    /// کاربر (پس از آن مقدار کاربر حفظ می‌شود).
    /// </summary>
    partial void OnNameFaChanged(string value)
    {
        if (_genericNameIsAuto)
            GenericName = value;
    }

    /// <summary>ویرایش دستی ژنریک — پرچم خودکار خاموش می‌شود</summary>
    partial void OnGenericNameChanged(string value)
    {
        // مقدار خالی (پاک‌کردن کاربر) دوباره رونوشت خودکار را فعال می‌کند
        if (string.IsNullOrEmpty(value)) return;
        if (_genericNameIsAuto && !string.Equals(value, NameFa, StringComparison.Ordinal))
            _genericNameIsAuto = false;
    }

    /// <summary>ثبت قلم جدید — نوع از بافت فراخواننده می‌آید</summary>
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
        // گارد تک‌نویسه — بازدارندهٔ رونوشت ناقص (درس چرخهٔ ۴۳؛ سرور هم جداگانه می‌سنجد)
        if (GenericName.Trim().Length < 2)
        {
            ErrorMessage = "نام ژنریک حداقل ۲ نویسه است — اگر نام فارسی خودکار کپی شده، آن را کامل کنید";
            return;
        }

        IsBusy = true;
        // ═══ چرخهٔ ۴۱ — مسیر مستقل api/v1/consumables/items ═══
        // همان فرمان CreatePharmacyItemCommand چرخهٔ ۳۹ است؛ گیت نقش (Admin/Manager)
        // دست‌نخورده است. چرخهٔ ۴۳: itemKind از بافت فرم می‌آید نه از کمبوی حذف‌شده.
        var ok = await _api.PostAsync("consumables/items", new
        {
            nameFa = NameFa.Trim(),
            genericName = GenericName.Trim(),
            itemKind = _isConsumableContext ? 2 : 1, // ۱ دارو، ۲ لوازم مصرفی
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
