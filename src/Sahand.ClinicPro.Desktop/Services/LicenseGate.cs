namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// پاکت پاسخ endpoint وضعیت لایسنس (license/status) — چرخه هفدهم (گزارش یازدهم مالک).
/// عمومی است تا هم ViewModelها (واقعی) و هم تست‌های واحد (پاسخ ساختگی) از یک شکل
/// مشترک deserialize کنند؛ نام‌گذاری camelCase سرور با گزینه‌های حساس-به-حروف-نفی
/// ApiClient (PropertyNameCaseInsensitive) نگاشت می‌شود.
/// </summary>
public sealed class LicenseStatusEnvelope
{
    /// <summary>پرچم موفقیت پاکت استاندارد ApiResponse</summary>
    public bool IsSuccess { get; set; }

    /// <summary>بدنهٔ داده — وضعیت لایسنس</summary>
    public LicenseStatusData? Data { get; set; }
}

/// <summary>دادهٔ وضعیت لایسنس — فیلدهای مورد نیاز گیت ورود + پیام سرور</summary>
public sealed class LicenseStatusData
{
    /// <summary>لایسنس معتبر و فعال است؟</summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// پیام سرور لایسنس — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
    /// «پیام‌های سرور لایسنس در برنامه نمایش داده شود» — پیام متنی پنل
    /// (مانند هشدار نزدیکی انقضا یا اطلاعیهٔ فروشنده) که سرور API در
    /// پاسخ license/status برمی‌گرداند و پیش‌تر نادیده گرفته می‌شد.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>تاریخ انقضا — برای شمارش روزهای باقیمانده در نوار وضعیت</summary>
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// گیت لایسنس — چرخه هفدهم (گزارش یازدهم مالک).
/// پیش‌تر با زدن رمز عبور، بدون فعال‌سازی لایسنس هم وارد پوستهٔ اصلی می‌شدند و
/// دکمهٔ «فعال‌سازی لایسنس» صفحهٔ ورود هیچ شنونده‌ای نداشت (کاملاً بی‌اثر بود).
/// این کلاس تعبیر مشترک وضعیت لایسنس را متمرکز می‌کند تا هر سه نقطهٔ ورود
/// (لاگین، تغییر رمز اجباری، اجرای برنامه با توکن ذخیره‌شده) از یک قانون واحد
/// پیروی کنند: لایسنس نامعتبر ⇒ پنجرهٔ فعال‌سازی، لایسنس معتبر یا «نامعلوم به
/// دلیل خطای شبکه» ⇒ ادامهٔ کار (درمانگاه نباید بر اثر قطعیِ لحظه‌ای سرورِ
/// لایسنس از کار بیفتد — fail-open فقط برای وضعیت نامعلوم، نه لایسنس ردشده).
/// </summary>
public static class LicenseGate
{
    /// <summary>وضعیت لایسنس پس از یک بررسی</summary>
    public enum Verdict
    {
        /// <summary>لایسنس معتبر است — ورود مجاز</summary>
        Valid,

        /// <summary>سرور صریحاً اعلام کرده لایسنس نامعتبر است — فعال‌سازی الزامی</summary>
        Invalid,

        /// <summary>وضعیت قابل تعیین نبود (قطعی شبکه/خطای سرور) — ورود مجاز (fail-open)</summary>
        Unknown,
    }

    /// <summary>
    /// دریافت و تعبیر وضعیت لایسنس از سرور — پاسخ null (خطای شبکه) به Unknown
    /// ترجمه می‌شود؛ پاسخ دارای Data طبق IsValid داوری می‌شود.
    /// </summary>
    public static async Task<Verdict> CheckAsync(IApiClient api, CancellationToken ct = default)
    {
        var envelope = await api.GetAsync<LicenseStatusEnvelope>("license/status", ct);
        return envelope?.Data switch
        {
            { IsValid: true } => Verdict.Valid,
            { IsValid: false } => Verdict.Invalid,
            _ => Verdict.Unknown,
        };
    }

    /// <summary>
    /// نتیجهٔ بررسی تفصیلی لایسنس — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
    /// علاوه بر داوری، پیام سرور و روزهای باقیمانده هم برگردانده می‌شود تا
    /// نوار وضعیت برنامه و اعلان سراسری، پیام پنل لایسنس را به کاربر برسانند.
    /// </summary>
    public sealed record LicenseCheckResult(Verdict Verdict, string? Message, DateTime? ExpiryDate)
    {
        /// <summary>شمار روزهای باقیمانده تا انقضا — تهی یعنی دائمی/نامعلوم</summary>
        public int? DaysRemaining => ExpiryDate is { } d
            ? (int)(d.Date - IranClock.Now.Date).TotalDays
            : null;
    }

    /// <summary>
    /// بررسی تفصیلی — همان CheckAsync اما پیام و انقضای سرور را هم برمی‌گرداند.
    /// خطای شبکه ⇒ Unknown با پیام تهی (درمانگاه نباید به‌خاطر قطعی لحظه‌ای از
    /// کار بیفتد — همان سیاست fail-open CheckAsync).
    /// </summary>
    public static async Task<LicenseCheckResult> CheckDetailedAsync(IApiClient api, CancellationToken ct = default)
    {
        var envelope = await api.GetAsync<LicenseStatusEnvelope>("license/status", ct);
        return envelope?.Data switch
        {
            { IsValid: true } d => new LicenseCheckResult(Verdict.Valid, d.Message, d.ExpiryDate),
            { IsValid: false } d => new LicenseCheckResult(Verdict.Invalid, d.Message, d.ExpiryDate),
            _ => new LicenseCheckResult(Verdict.Unknown, null, null),
        };
    }
}
