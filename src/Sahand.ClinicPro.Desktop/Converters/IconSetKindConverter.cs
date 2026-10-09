using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل مجموعهٔ آیکون‌ها — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
/// «تنظیمات تم: پشتیبانی تعویض یکجای مجموعهٔ آیکون‌ها (نه فقط تغییر رنگ)».
/// نام آیکون پایهٔ هر آیتم منو با توجه به مجموعهٔ منتخب کاربر (کلاسیک/
/// مدرن/مینیمال — منبع پویای App.IconSetName از ThemeService) به نام
/// هم‌ارز همان مجموعه نگاشت می‌شود تا شکل واقعی آیکون‌ها عوض شود.
/// ▪ MultiBinding است تا با شمارندهٔ IconRefreshTick (که ThemeService با
///   هر تغییر ظاهری زیاد می‌کند) بی‌درنگ و بدون ری‌استارت دوباره ارزیابی شود
///   (همان الگوی IconTintConverter چرخه ۲۴).
/// ▪ اگر نگاشتی برای آیکونی در مجموعهٔ منتخب نبود، همان نام پایه برگردانده
///   می‌شود (fallback ایمن) — هیچ آیکونی گم نمی‌شود.
/// خروجی رشته است؛ موتور اتصال WPF آن را با TypeConverter از پیش‌فرضِ
/// PackIconKind به enum تبدیل می‌کند (همان رفتار Kind="{Binding Icon}" فعلی).
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
    /// نام پایه → نام مجموعهٔ فعال. مقدار دوم (شمارندهٔ نوسازی) فقط برای
    /// تحریک ارزیابی مجدد است؛ خوانده نمی‌شود.
    /// </summary>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not string baseIcon || string.IsNullOrEmpty(baseIcon))
            return "ChartLine"; // آیکون پیش‌فرض امن پک‌آیکون

        // مجموعهٔ فعال از منبع پویا — پیش‌فرض کلاسیک (بدون تغییر نام)
        var setName = System.Windows.Application.Current?.Resources["App.IconSetName"] as string
                      ?? "Classic";

        var map = setName switch
        {
            "Modern" => Modern,
            "Minimal" => Minimal,
            _ => null // کلاسیک = همان نام پایه
        };

        return map is not null && map.TryGetValue(baseIcon, out var mapped)
            ? mapped // نگاشت یافت شد
            : baseIcon; // fallback ایمن — آیکون پایه
    }

    /// <summary>تبدیل معکوس لازم نیست — اتصال یک‌طرفه است</summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => Array.Empty<object>();
}
