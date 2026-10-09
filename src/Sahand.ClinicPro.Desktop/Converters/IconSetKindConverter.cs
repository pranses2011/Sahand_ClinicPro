using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل مجموعهٔ آیکون‌ها — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
/// «تنظیمات تم: پشتیبانی تعویض یکجای مجموعهٔ آیکون‌ها (نه فقط تغییر رنگ)».
/// نام آیکون پایهٔ هر آیتم منو با توجه به مجموعهٔ منتخب کاربر (کلاسیک/
/// مدرن/مینیمال — منبع پویای App.IconSetName از ThemeService) به نام
/// هم‌ارز همان مجموعه نگاشت می‌شود تا شکل واقعی آیکون‌ها عوض شود.
/// ═══════════════════════════════════════════════════════════════════
/// چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک — «آیکون‌های منو خراب شده و همه با
/// یک آیکون نشان داده می‌شوند»):
/// ریشه — این مبدل پیش‌تر «رشتهٔ نام آیکون» برمی‌گرداند و انتظار داشت
/// موتور اتصال WPF آن را خودکار به enum تبدیل کند؛ اما برخلاف اتصال
/// تک‌مقداری (Binding ساده) که تبدیل رشته→enum را از طریق
/// TypeConverter انجام می‌دهد، در MultiBinding خروجی مبدل بدون تبدیل
/// خودکار روی خاصیت هدف (PackIconKind) می‌نشیند؛ اتصال شکست می‌خورد و
/// «همهٔ» آیکون‌ها روی مقدار پیش‌فرض enum می‌مانند — دقیقاً علامتِ
/// «همه با یک آیکون». رفع قطعی: خروجی از نوع PackIconKind (خودِ enum)
/// برگردانده می‌شود — بدون هیچ تبدیلی در لایهٔ اتصال.
/// </summary>
public class IconSetKindConverter : IMultiValueConverter
{
    /// <summary>
    /// نگاشت مجموعهٔ «مدرن» — نسخه‌های Outline/نرم‌تر آیکون‌های پایه؛
    /// حس بصری خط‌نازک (Line/Swift-شبیه) برای رابط‌های امروزی.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, string> Modern =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ViewDashboard"] = "ViewDashboardOutline",
            ["ViewDashboardOutline"] = "ViewDashboard",
            ["AccountGroup"] = "AccountMultipleOutline",
            ["AccountMultiple"] = "AccountMultipleOutline",
            ["CalendarClock"] = "CalendarClockOutline",
            ["ClipboardAccount"] = "ClipboardAccountOutline",
            ["Stethoscope"] = "Stethoscope",
            ["HeartPulse"] = "HeartOutline",
            ["Doctor"] = "Doctor",
            ["FileDocumentEdit"] = "FileDocumentEditOutline",
            ["FileDocumentEditOutline"] = "FileDocumentOutline",
            ["Pill"] = "Pill",
            ["ClipboardCheckOutline"] = "ClipboardCheckOutline",
            ["CloudUploadOutline"] = "CloudUploadOutline",
            ["TestTube"] = "TestTube",
            ["Prescription"] = "FileDocumentEditOutline",
            ["AccountTie"] = "AccountTieOutline",
            ["AccountTieOutline"] = "AccountTieOutline",
            ["CashMultiple"] = "CashMultiple",
            ["CashCheck"] = "CashCheck",
            ["BankTransfer"] = "BankTransfer",
            ["MedicalBag"] = "MedicalBag",
            ["Cog"] = "CogOutline",
            ["ShieldAccount"] = "ShieldAccountOutline",
            ["ShieldCheckOutline"] = "ShieldCheckOutline",
            ["MessageTextOutline"] = "MessageTextOutline",
            ["CommentQuoteOutline"] = "CommentQuoteOutline",
            ["BookOpenOutline"] = "BookOpenOutline",
            ["School"] = "School",
            ["HomeGroup"] = "HomeGroup",
            ["KeyChain"] = "KeyChain",
            ["DatabaseSync"] = "DatabaseSync",
            ["FileChart"] = "FileChart",
            ["TuneVertical"] = "TuneVertical",
            ["Fingerprint"] = "Fingerprint",
            ["AccountKey"] = "AccountKeyOutline",
            ["Hospital"] = "HospitalBuilding",
        };

    /// <summary>
    /// نگاشت مجموعهٔ «مینیمال» — ساده‌ترین گلیف هر مفهوم؛ رابط کم‌حاشیه و
    /// سبک برای نمایشگرهای کوچک/تراکم زیاد.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, string> Minimal =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ViewDashboard"] = "ViewGrid",
            ["ViewDashboardOutline"] = "ViewGrid",
            ["AccountGroup"] = "AccountMultiple",
            ["AccountMultiple"] = "AccountMultiple",
            ["AccountKey"] = "AccountKey",
            ["AccountTie"] = "Account",
            ["AccountTieOutline"] = "Account",
            ["CalendarClock"] = "Calendar",
            ["ClipboardAccount"] = "ClipboardText",
            ["Stethoscope"] = "MedicalBag",
            ["HeartPulse"] = "Heart",
            ["Doctor"] = "Account",
            ["FileDocumentEdit"] = "FileDocument",
            ["FileDocumentEditOutline"] = "FileDocument",
            ["FileDocumentOutline"] = "FileDocument",
            ["Prescription"] = "FileDocument",
            ["Pill"] = "Pill",
            ["ClipboardCheckOutline"] = "Clipboard",
            ["ClipboardCheck"] = "Clipboard",
            ["CloudUploadOutline"] = "Cloud",
            ["TestTube"] = "FlaskEmpty",
            ["CashMultiple"] = "Cash",
            ["CashCheck"] = "Cash",
            ["BankTransfer"] = "Bank",
            ["MedicalBag"] = "MedicalBag",
            ["Cog"] = "Cog",
            ["CogOutline"] = "Cog",
            ["ShieldAccount"] = "Shield",
            ["ShieldAccountOutline"] = "Shield",
            ["ShieldCheckOutline"] = "Shield",
            ["ShieldCheck"] = "Shield",
            ["MessageTextOutline"] = "Message",
            ["CommentQuoteOutline"] = "Comment",
            ["BookOpenOutline"] = "Book",
            ["BookOpen"] = "Book",
            ["School"] = "School",
            ["HomeGroup"] = "Home",
            ["KeyChain"] = "Key",
            ["DatabaseSync"] = "Database",
            ["FileChart"] = "ChartBar",
            ["TuneVertical"] = "Tune",
            ["Fingerprint"] = "Fingerprint",
            ["Hospital"] = "Domain",
        };

    /// <summary>
    /// نام پایه → عضو enum پک‌آیکون مجموعهٔ فعال. مقدار دوم (شمارندهٔ نوسازی)
    /// فقط برای تحریک ارزیابی مجدد است؛ خوانده نمی‌شود.
    /// خروجی «همیشه» از نوع PackIconKind است (چرخه ۲۸ — رفع یکسان‌شدن
    /// آیکون‌ها؛ توضیح کامل در سربرگ کلاس).
    /// </summary>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // نام پایهٔ آیکون — اگر به هر دلیل وصل نشد (تهی/خالی/غیررشته)،
        // آیکون امن پیش‌فرض؛ هرگز رشتهٔ خام به اتصال enum برنمی‌گردد
        if (values is not { Length: > 0 } || values[0] is not string baseIcon || string.IsNullOrEmpty(baseIcon))
            return PackIconKind.ChartLine;

        // مجموعهٔ فعال از منبع پویا — پیش‌فرض کلاسیک (بدون تغییر نام)
        var setName = System.Windows.Application.Current?.Resources["App.IconSetName"] as string
                      ?? "Classic";

        var map = setName switch
        {
            "Modern" => Modern,
            "Minimal" => Minimal,
            _ => null // کلاسیک = همان نام پایه
        };

        var targetName = map is not null && map.TryGetValue(baseIcon, out var mapped)
            ? mapped // نگاشت یافت شد
            : baseIcon; // fallback ایمن — آیکون پایه

        // تبدیل قطعی نام → enum با پارس امن؛ اگر نام (به هر دلیل، مثلاً
        // نسخهٔ آیندهٔ کتابخانه) عضو enum نبود، اول نام پایه و سپس آیکون
        // امن پیش‌فرض — هرگز مقدار رشته‌ای برگردانده نمی‌شود
        if (Enum.TryParse<PackIconKind>(targetName, true, out var kind))
            return kind;
        if (Enum.TryParse<PackIconKind>(baseIcon, true, out var fallback))
            return fallback;
        return PackIconKind.ChartLine;
    }

    /// <summary>تبدیل معکوس لازم نیست — اتصال یک‌طرفه است</summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => Array.Empty<object>();
}
