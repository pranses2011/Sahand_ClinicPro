using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// نوع بخش نمایشگری سالن انتظار — چرخهٔ ۷۴.
/// هر بخش یک کارت مستقل در میانهٔ نمایشگر است؛ نوعش منبع داده را تعیین می‌کند.
/// </summary>
public enum WaitingRoomSectionType
{
    /// <summary>صف ویزیت یک پزشک (یا همهٔ پزشکان) — از visits/queue</summary>
    DoctorQueue = 1,

    /// <summary>صف یک خدمت درمانی (دندانپزشکی/چشم‌پزشکی/تزریقات/…) — از receptions/today با فیلتر خدمت</summary>
    ServiceQueue = 2,

    /// <summary>نوبت‌های امروز درمانگاه — از dashboard/appointments-today</summary>
    TodayAppointments = 3,
}

/// <summary>
/// یک بخش نمایشگری — کارت مستقل با عنوان دلخواه و پیکربندی نوع‌محور.
/// </summary>
public class WaitingRoomSection
{
    /// <summary>نوع بخش — تعیین‌کنندهٔ منبع داده</summary>
    [JsonPropertyName("type")]
    public WaitingRoomSectionType Type { get; set; } = WaitingRoomSectionType.DoctorQueue;

    /// <summary>عنوان دلخواه روی کارت — مثلاً «صف دندانپزشکی»</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>شناسهٔ پزشک — فقط برای DoctorQueue (تهی = همهٔ پزشکان)</summary>
    [JsonPropertyName("doctorId")]
    public long? DoctorId { get; set; }

    /// <summary>نام پزشک — برای نمایش در تنظیمات (شناسه مبنای کار است)</summary>
    [JsonPropertyName("doctorName")]
    public string? DoctorName { get; set; }

    /// <summary>شناسهٔ خدمت — فقط برای ServiceQueue</summary>
    [JsonPropertyName("serviceId")]
    public long? ServiceId { get; set; }

    /// <summary>نام خدمت — برای نمایش در تنظیمات</summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }
}

/// <summary>
/// پیکربندی کامل نمایشگر سالن انتظار — چرخهٔ ۷۴.
/// گزارش مالک: «برای نمایشگر سالن انتظار تنظیمات هم بزار تا بتونیم تعیین
/// بکنیم که چه چیزی رو نمایش بده… و همچنین برای سایر مواردی که میشه ازش در
/// نمایشگر سالن انتظار استفاده کرد مانند تنظیمات ساعت آنالوگ و ساعت دیجیتال
/// و تاریخ. فقط اینو در نظر داشته باش که تبلیغات نرم افزار همیشه عضو ثابت
/// این صفحه باشه و بقیه موارد رو بشه تنظیم کرد و همچنین میخوام این تنظیمات
/// بصورت Drag &amp; Drop پیاده سازی بشه و پیش نمایش هم داشته باشه».
/// ذخیره‌سازی محلی است (LocalSettingsStore) چون نمایشگر یک ایستگاه/کیوسک است
/// نه کاربر — تنظیم نمایشگرِ تلویزیون سالن به حساب کاربرِ واردشده وابسته نیست.
/// </summary>
public class WaitingRoomConfig
{
    /// <summary>نام درمانگاه برای سربرگ — خالی یعنی نام همگام‌شده از سرور لایسنس</summary>
    [JsonPropertyName("clinicName")]
    public string ClinicName { get; set; } = string.Empty;

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — اعلان زیر نام درمانگاه (گزارش مالک) ═══
    /// «نوشته اعلان زیر نام درمانگاه هم از تنظیمات قابل تغییر باشه» —
    /// متن دلخواه اپراتور (مثلاً «امروز ویزیت اطفال برقرار است»)؛ خالی = متن
    /// پیش‌فرض مؤدبانهٔ «لطفاً در سالن انتظار حضور داشته باشید…».
    /// </summary>
    [JsonPropertyName("announcement")]
    public string Announcement { get; set; } = string.Empty;

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — تعداد ستون‌های ناحیهٔ صف‌ها (گزارش مالک) ═══
    /// «مواردی که نمایشگر اضافه میشن … بصورت ستونی قرار بگیرن نه سطری و بشه
    /// در یک ستون ۲ مورد هم قرار داد (تعداد ستون‌ها هم از تنظیماتش قابل
    /// تنظیم باشه)» — دامنهٔ ۱ تا ۴؛ پیش‌فرض ۳.
    /// </summary>
    [JsonPropertyName("columnCount")]
    public int ColumnCount { get; set; } = 3;

    /// <summary>نام دستگاه نمایشگر هدف (پنجره روی همان مانیتور/تلویزیون باز می‌شود)</summary>
    [JsonPropertyName("screenDeviceName")]
    public string ScreenDeviceName { get; set; } = string.Empty;

    /// <summary>ساعت آنالوگ نمایش داده شود؟</summary>
    [JsonPropertyName("showAnalogClock")]
    public bool ShowAnalogClock { get; set; } = true;

    /// <summary>ساعت دیجیتال (کوچک، زیر آنالوگ) نمایش داده شود؟</summary>
    [JsonPropertyName("showDigitalClock")]
    public bool ShowDigitalClock { get; set; } = true;

    /// <summary>تاریخ بلند (تاریخ امروز : ۲۳ مهر ماه ۱۴۰۵) نمایش داده شود؟</summary>
    [JsonPropertyName("showLongDate")]
    public bool ShowLongDate { get; set; } = true;

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — تبلیغ فقط تصویر + نام محصول (گزارش مالک) ═══
    /// «در تبلیغات فقط تصویر و نام نرم‌افزار کفایت میکنه و بقیه موارد رو در
    /// تنظیمات برای نشون دادن بزار که انتخابی باشه» — سه سوییچ برای اقلام
    /// اختیاری: زیرنویس «سامانهٔ جامع»، فهرست ویژگی‌ها و نسخهٔ محصول.
    /// </summary>
    [JsonPropertyName("showAdSubtitle")]
    public bool ShowAdSubtitle { get; set; } = false;

    /// <summary>فهرست ویژگی‌های تبلیغ — انتخابی (پیش‌فرض خاموش)</summary>
    [JsonPropertyName("showAdFeatures")]
    public bool ShowAdFeatures { get; set; } = false;

    /// <summary>نسخهٔ محصول در پایین تبلیغ — انتخابی (پیش‌فرض خاموش)</summary>
    [JsonPropertyName("showAdVersion")]
    public bool ShowAdVersion { get; set; } = false;

    /// <summary>بخش‌های میانه — ترتیب همان ترتیبِ Drag &amp; Drop تنظیمات است</summary>
    [JsonPropertyName("sections")]
    public List<WaitingRoomSection> Sections { get; set; } = new();

    /// <summary>کلید ذخیره در انبار محلی</summary>
    private const string StoreKey = "WaitingRoom.Config";

    /// <summary>پیکربندی پیش‌فرض — صف همهٔ پزشکان + نوبت‌های امروز</summary>
    public static WaitingRoomConfig Default() => new()
    {
        Sections = new List<WaitingRoomSection>
        {
            new()
            {
                Type = WaitingRoomSectionType.DoctorQueue,
                Title = "صف انتظار ویزیت",
            },
            new()
            {
                Type = WaitingRoomSectionType.TodayAppointments,
                Title = "نوبت‌های امروز",
            },
        },
    };

    /// <summary>بارگذاری از انبار محلی — نبود/خرابی = پیش‌فرض (نمایشگر هرگز بی‌تنظیم نمی‌ماند)</summary>
    public static WaitingRoomConfig Load()
    {
        try
        {
            var json = LocalSettingsStore.Get(StoreKey);
            if (string.IsNullOrWhiteSpace(json))
                return Default();
            var config = JsonSerializer.Deserialize<WaitingRoomConfig>(json);
            return config ?? Default();
        }
        catch
        {
            return Default();
        }
    }

    /// <summary>ذخیره در انبار محلی — شکست بی‌صدا (نمایشگر با پیکربندی حافظه ادامه می‌دهد)</summary>
    public void Save()
    {
        try
        {
            LocalSettingsStore.Set(StoreKey, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // ذخیرهٔ تنظیمات هرگز جریان نمایشگر را متوقف نمی‌کند
        }
    }

    /// <summary>
    /// نام درمانگاه مؤثر — اولویت: نام مستقل تنظیمات ← نام چاپی ← «درمانگاه سهند».
    /// چرخهٔ ۷۵: مقدار نمایشی اولیه همین است؛ بلافاصله پس از باز شدن پنجره،
    /// نام همگام‌شده از سرور لایسنس (تنظیم ClinicInfo) هم خوانده می‌شود و اگر
    /// اپراتور نام دستی ننوشته باشد جایگزین می‌شود — «نام درمانگاه همان نامی
    /// است که از سرور لایسنس می‌آید».
    /// </summary>
    public string EffectiveClinicName()
    {
        if (!string.IsNullOrWhiteSpace(ClinicName))
            return ClinicName.Trim();
        var printerName = LocalSettingsStore.Get("Printer.ClinicName");
        return string.IsNullOrWhiteSpace(printerName) ? "درمانگاه" : printerName.Trim();
    }
}
