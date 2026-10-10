namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// ورودی کاتالوگ صفحات — یک صفحهٔ ناودارانهٔ کامل
/// </summary>
/// <param name="Key">کلید صفحه — همان کلیدهای کارخانهٔ MainWindowViewModel</param>
/// <param name="Title">عنوان فارسی صفحه — روی کاشی میانبر و در انتخابگر</param>
/// <param name="Icon">آیکون PackIcon متریال‌دیزاین</param>
/// <param name="Permission">کلید مجوز لازم — تهی یعنی آزاد (بدون گیت سرور)</param>
/// <param name="ShowInPicker">آیا در انتخابگر میانبر/میانبرهای داشبورد دیده شود؟ (صفحات داخلی/تکراری خیر)</param>
public record PageCatalogEntry(
    string Key,
    string Title,
    string Icon,
    string? Permission,
    bool ShowInPicker = true);

/// <summary>
/// کاتالوگ یکپارچهٔ صفحات برنامه — چرخهٔ سی‌ونه‌م (گزارش مالک)
/// </summary>
/// <remarks>
/// «میخوام در داشبورد… بتونم دکمه‌های میانبر برای قسمت‌های مختلف رو داخلش
/// ایجاد بکنم… با کلیک بر روی اون دکمه مستقیم اون فرم باز بشه» — این کاتالوگ
/// «منبع یگانهٔ حقیقت» برای سه مصرف‌کننده است تا کلید/عنوان/آیکون/مجوز هیچ‌وقت
/// در دو جا از هم جدا نیفتد:
///   ۱) نقشهٔ مجوز منوی کناری (MainWindowViewModel.PagePermissionMap)
///   ۲) انتخابگر میانبرهای داشبورد (ShortcutPickerWindow)
///   ۳) کاشی‌های میانبر داشبورد (نمایش عنوان/آیکون)
/// مجوزها همان گیت‌های HasPermission خود کنترلرها هستند؛ صفحات آزاد (لایسنس/
/// آموزش) مجوز تهی دارند و از فیلتر مجوز مستثنا می‌مانند.
/// </remarks>
public static class PageCatalog
{
    /// <summary>همهٔ صفحات ناودارانهٔ برنامه</summary>
    public static readonly IReadOnlyList<PageCatalogEntry> All = new[]
    {
        // ─── جریان درمان ───
        new PageCatalogEntry("dashboard", "داشبورد", "ViewDashboard", "dashboard.view"),
        new PageCatalogEntry("patients", "بیماران", "AccountGroup", "patients.view"),
        new PageCatalogEntry("appointments", "نوبت‌دهی", "CalendarClock", "appointments.view"),
        new PageCatalogEntry("receptions", "پذیرش", "ClipboardAccount", "receptions.view"),
        new PageCatalogEntry("visits", "ویزیت و معاینه", "Stethoscope", "visits.view"),
        new PageCatalogEntry("doctor-visit", "میزکار پزشک", "Doctor", "visits.view"),
        new PageCatalogEntry("emr-advanced", "پرونده بالینی پیشرفته", "HeartPulse", "emr.advanced"),

        // ─── دارو و انبار ───
        new PageCatalogEntry("prescriptions", "نسخه‌نویسی", "FileDocumentEdit", "prescriptions.view"),
        new PageCatalogEntry("pharmacy", "داروخانه و انبار", "Pill", "pharmacy.view"),
        new PageCatalogEntry("stocktaking", "انبارگردانی", "ClipboardCheckOutline", "pharmacy.stocktake"),
        new PageCatalogEntry("erx", "نسخه الکترونیک", "CloudUploadOutline", "prescriptions.view"),

        // ─── پاراکلینیک ───
        new PageCatalogEntry("lab", "آزمایشگاه", "TestTube", "lab.view"),
        new PageCatalogEntry("paraclinical", "تصویربرداری و پاراکلینیک", "MedicalBag", "imaging.view"),

        // ─── کادر درمان و پرسنل ───
        new PageCatalogEntry("doctors", "پزشکان", "Doctor", "doctors.view"),
        new PageCatalogEntry("personnel", "پرسنل", "AccountTie", "patients.view"),
        new PageCatalogEntry("payroll", "فیش حقوقی", "CashCheck", "payroll.manage"),
        new PageCatalogEntry("attendance", "حضور و غیاب", "Fingerprint", "attendance.view"),

        // ─── مالی و بیمه ───
        new PageCatalogEntry("billing", "مالی و صندوق", "CashMultiple", "invoices.view"),
        new PageCatalogEntry("accounting", "حسابداری", "BookOpenOutline", "accounting.view"),
        new PageCatalogEntry("insurance", "بیمه", "ShieldCheckOutline", "insurance.view"),
        new PageCatalogEntry("sepas", "سپاس", "BankTransfer", "appointments.view"),

        // ─── ارتباط با بیمار ───
        new PageCatalogEntry("sms", "پیامک", "MessageTextOutline", "sms.view"),
        new PageCatalogEntry("surveys", "نظرسنجی بیماران", "CommentQuoteOutline", "surveys.view"),

        // ─── گزارش‌ها ───
        new PageCatalogEntry("reports", "گزارش‌ها", "FileChart", "reports.view"),

        // ─── مدیریت سامانه ───
        new PageCatalogEntry("admin", "مدیریت سامانه (تب‌بندی کامل)", "ViewDashboardOutline", "settings.view"),
        new PageCatalogEntry("users", "کاربران", "AccountKey", "users.view"),
        new PageCatalogEntry("roles", "نقش‌ها", "ShieldAccount", "roles.view"),
        new PageCatalogEntry("settings", "تنظیمات", "Cog", "settings.view"),
        // settings-full همان صفحهٔ تنظیمات است — در انتخابگر نمی‌آید (کلید ناوبری حفظ شده)
        new PageCatalogEntry("settings-full", "تنظیمات (کامل)", "Cog", "settings.view", ShowInPicker: false),
        new PageCatalogEntry("branches", "شعبه‌ها", "HomeGroup", "settings.view"),
        new PageCatalogEntry("rooms", "مدیریت اتاق‌ها", "Door", "settings.view"),
        new PageCatalogEntry("backup", "پشتیبان‌گیری و بازیابی", "DatabaseSync", "backup.view"),
        new PageCatalogEntry("audit", "لاگ عملیات", "FileDocumentEditOutline", "auditlogs.view"),
        new PageCatalogEntry("complementary", "قالب چاپ و نظرسنجی", "TuneVertical", "settings.view"),
        new PageCatalogEntry("license", "لایسنس", "KeyChain", null),
        new PageCatalogEntry("training", "🎓 مرکز آموزش", "School", null),

        // ─── داخلی — در انتخابگر نمی‌آیند ───
        new PageCatalogEntry("notifications", "اعلان‌ها", "BellOutline", null, ShowInPicker: false),
        new PageCatalogEntry("reports-legacy", "گزارش‌ها (قدیمی)", "FileChart", "reports.view", ShowInPicker: false),
    };

    /// <summary>یافتن ورودی با کلید — تهی یعنی کلید ناشناخته</summary>
    public static PageCatalogEntry? Find(string key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.Ordinal));

    /// <summary>
    /// نقشهٔ کلید ← مجوز برای فیلتر منو — فقط صفحاتِ دارای گیت؛ صفحات آزاد
    /// (مجوز تهی) عمداً در نقشه نیستند تا رفتار «نبودن در نقشه = آزاد» حفظ شود
    /// (همان قرارداد پیشین MainWindowViewModel).
    /// </summary>
    public static IReadOnlyDictionary<string, string> PermissionMap { get; } =
        All.Where(p => p.Permission is not null)
           .ToDictionary(p => p.Key, p => p.Permission!, StringComparer.Ordinal);
}
