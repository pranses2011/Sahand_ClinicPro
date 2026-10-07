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

/// <summary>دادهٔ وضعیت لایسنس — فقط فیلدهایی که گیت ورود نیاز دارد</summary>
public sealed class LicenseStatusData
{
    /// <summary>لایسنس معتبر و فعال است؟</summary>
    public bool IsValid { get; set; }
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
}
