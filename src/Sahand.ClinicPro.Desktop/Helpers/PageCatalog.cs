namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// ورودی کاتالوگ صفحات — یک صفحهٔ ناودارانهٔ کامل
/// </summary>
/// <param name="Key">کلید صفحه — همان کلیدهای کارخانهٔ MainWindowViewModel</param>
/// <param name="Title">عنوان فارسی صفحه — روی کاشی میانبر و در انتخابگر</param>
/// <param name="Icon">آیکون PackIcon متریال‌دیزاین</param>
/// <param name="Permission">کلید مجوز لازم — تهی یعنی آزاد (بدون گیت سرور)</param>
/// <param name="ShowInPicker">آیا در انتخابگر میانبر/میانبرهای داشبورد دیده شود؟ (صفحات داخلی/تکراری خیر)</param>
/// <param name="Group">نام فارسی دستهٔ منو — چرخهٔ ۴۲: گروه‌بندی و رنگبندی انتخابگر میانبرها؛ تهی یعنی «مدیریت سامانه» (پیش‌فرضِ امنِ قدیمی)</param>
public record PageCatalogEntry(
    string Key,
    string Title,
    string Icon,
    string? Permission,
    bool ShowInPicker = true,
    string Group = "مدیریت سامانه");

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
/// ═══ چرخهٔ چهل‌ودوم (گزارش مالک: «در فرم انتخاب میانبرهای داشبورد ظاهر
/// بدی داره و زیباتر و حرفه‌ای‌ترش بکن») — پارامتر Group و دیکشنری GroupColors
/// اضافه شد: انتخابگر میانبرها صفحات را بر اساس همین دسته‌ها گروه‌بندی و
/// رنگ‌آمیزی می‌کند تا یافتن صفحه در فهرست بلند، آسان و لذت‌بخش شود.
/// </remarks>
public static class PageCatalog
{
    /// <summary>همهٔ صفحات ناودارانهٔ برنامه — با دستهٔ منو (چرخهٔ ۴۲)</summary>
    public static readonly IReadOnlyList<PageCatalogEntry> All = new[]
    {
        // ─── جریان درمان ───
        new PageCatalogEntry("dashboard", "داشبورد", "ViewDashboard", "dashboard.view", Group: "جریان درمان"),
        new PageCatalogEntry("patients", "بیماران", "AccountGroup", "patients.view", Group: "جریان درمان"),
        new PageCatalogEntry("appointments", "نوبت‌دهی", "CalendarClock", "appointments.view", Group: "جریان درمان"),
        new PageCatalogEntry("receptions", "پذیرش", "ClipboardAccount", "receptions.view", Group: "جریان درمان"),
        // چرخهٔ ۴۱ — لوازم مصرفی درمانگاه: صفحهٔ مستقلِ مدیریت مصرفی‌ها؛ مجوزش
        // همان پذیرش است چون مصرفِ سرنگ/سرم/گاز حین خدمت، حلقهٔ طبیعیِ جریان
        // پذیرش است و درمانگاهِ بدون ماژول داروخانه هم باید به آن دسترسی داشته باشد
        new PageCatalogEntry("consumables", "لوازم مصرفی درمانگاه", "Syringe", "receptions.view", Group: "جریان درمان"),
        new PageCatalogEntry("visits", "ویزیت و معاینه", "Stethoscope", "visits.view", Group: "جریان درمان"),
        new PageCatalogEntry("doctor-visit", "میزکار پزشک", "Doctor", "visits.view", Group: "جریان درمان"),
        new PageCatalogEntry("emr-advanced", "پرونده بالینی پیشرفته", "HeartPulse", "emr.advanced", Group: "جریان درمان"),

        // ─── دارو و انبار ───
        new PageCatalogEntry("prescriptions", "نسخه‌نویسی", "FileDocumentEdit", "prescriptions.view", Group: "دارو و انبار"),
        new PageCatalogEntry("pharmacy", "داروخانه و انبار", "Pill", "pharmacy.view", Group: "دارو و انبار"),
        new PageCatalogEntry("stocktaking", "انبارگردانی", "ClipboardCheckOutline", "pharmacy.stocktake", Group: "دارو و انبار"),
        new PageCatalogEntry("erx", "نسخه الکترونیک", "CloudUploadOutline", "prescriptions.view", Group: "دارو و انبار"),

        // ─── پاراکلینیک ───
        new PageCatalogEntry("lab", "آزمایشگاه", "TestTube", "lab.view", Group: "پاراکلینیک"),
        new PageCatalogEntry("paraclinical", "تصویربرداری و پاراکلینیک", "MedicalBag", "imaging.view", Group: "پاراکلینیک"),

        // ─── کادر درمان و پرسنل ───
        new PageCatalogEntry("doctors", "پزشکان", "Doctor", "doctors.view", Group: "کادر درمان و پرسنل"),
        new PageCatalogEntry("personnel", "پرسنل", "AccountTie", "patients.view", Group: "کادر درمان و پرسنل"),
        new PageCatalogEntry("payroll", "فیش حقوقی", "CashCheck", "payroll.manage", Group: "کادر درمان و پرسنل"),
        new PageCatalogEntry("attendance", "حضور و غیاب", "Fingerprint", "attendance.view", Group: "کادر درمان و پرسنل"),

        // ─── مالی و بیمه ───
        new PageCatalogEntry("billing", "مالی و صندوق", "CashMultiple", "invoices.view", Group: "مالی و بیمه"),
        new PageCatalogEntry("accounting", "حسابداری", "BookOpenOutline", "accounting.view", Group: "مالی و بیمه"),
        new PageCatalogEntry("insurance", "بیمه", "ShieldCheckOutline", "insurance.view", Group: "مالی و بیمه"),
        new PageCatalogEntry("sepas", "سپاس", "BankTransfer", "appointments.view", Group: "مالی و بیمه"),

        // ─── ارتباط با بیمار ───
        new PageCatalogEntry("sms", "پیامک", "MessageTextOutline", "sms.view", Group: "ارتباط با بیمار"),
        new PageCatalogEntry("surveys", "نظرسنجی بیماران", "CommentQuoteOutline", "surveys.view", Group: "ارتباط با بیمار"),

        // ─── گزارش‌ها ───
        new PageCatalogEntry("reports", "گزارش‌ها", "FileChart", "reports.view", Group: "گزارش‌ها"),

        // ─── مدیریت سامانه ───
        new PageCatalogEntry("admin", "مدیریت سامانه (تب‌بندی کامل)", "ViewDashboardOutline", "settings.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("users", "کاربران", "AccountKey", "users.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("roles", "نقش‌ها", "ShieldAccount", "roles.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("settings", "تنظیمات", "Cog", "settings.view", Group: "مدیریت سامانه"),
        // settings-full همان صفحهٔ تنظیمات است — در انتخابگر نمی‌آید (کلید ناوبری حفظ شده)
        new PageCatalogEntry("settings-full", "تنظیمات (کامل)", "Cog", "settings.view", ShowInPicker: false, Group: "مدیریت سامانه"),
        new PageCatalogEntry("branches", "شعبه‌ها", "HomeGroup", "settings.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("rooms", "مدیریت اتاق‌ها", "Door", "settings.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("backup", "پشتیبان‌گیری و بازیابی", "DatabaseSync", "backup.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("audit", "لاگ عملیات", "FileDocumentEditOutline", "auditlogs.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("complementary", "قالب چاپ و نظرسنجی", "TuneVertical", "settings.view", Group: "مدیریت سامانه"),
        new PageCatalogEntry("license", "لایسنس", "KeyChain", null, Group: "راهنما و لایسنس"),
        new PageCatalogEntry("training", "🎓 مرکز آموزش", "School", null, Group: "راهنما و لایسنس"),

        // ─── داخلی — در انتخابگر نمی‌آیند ───
        new PageCatalogEntry("notifications", "اعلان‌ها", "BellOutline", null, ShowInPicker: false, Group: "داخلی"),
        new PageCatalogEntry("reports-legacy", "گزارش‌ها (قدیمی)", "FileChart", "reports.view", ShowInPicker: false, Group: "داخلی"),
    };

    /// <summary>
    /// رنگ تأکیدی هر دسته — چرخهٔ ۴۲: انتخابگر میانبرها لبهٔ کارت، دایرهٔ آیکون
    /// و چیپ دسته را با همین رنگ می‌کشد؛ کاشی‌های داشبورد به چرخهٔ رنگی تکراریِ
    /// خودشان می‌مانند. رنگ‌ها روی پس‌زمینهٔ روشن و تیره هر دو خوانا انتخاب
    /// شده‌اند (اشباع متوسط، روشنایی یکنواخت).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> GroupColors = new Dictionary<string, string>
    {
        ["جریان درمان"] = "#1565C0",
        ["دارو و انبار"] = "#00897B",
        ["پاراکلینیک"] = "#8E24AA",
        ["کادر درمان و پرسنل"] = "#5E35B1",
        ["مالی و بیمه"] = "#2E7D32",
        ["ارتباط با بیمار"] = "#F57C00",
        ["گزارش‌ها"] = "#C2185B",
        ["مدیریت سامانه"] = "#5D4037",
        ["راهنما و لایسنس"] = "#00838F",
        ["داخلی"] = "#616161",
    };

    /// <summary>یافتن رنگ دسته — دستهٔ ناشناخته خاکستری می‌شود</summary>
    public static string ColorOfGroup(string? group) =>
        group is not null && GroupColors.TryGetValue(group, out var color) ? color : "#616161";

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
