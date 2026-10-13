using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sahand.ClinicPro.API.Common.Json;

/// <summary>
/// مبدل JSON «سهل‌گیر» برای TimeOnly — چرخهٔ ۶۷ (گزارش مالک:
/// «در مدیریت شیفت‌ها و ثبت شیفت خطای "اطلاعات ارسالی نامعتبر است" میده»).
/// </summary>
/// <remarks>
/// ═══ ریشهٔ باگ ═══
/// پشتیبانی بومی TimeOnly در System.Text.Json از نسخهٔ net8.0 موجود است اما
/// «دیسریال‌سازی» فقط قالب کامل «HH:mm:ss(.fffffff)» را می‌پذیرد؛ فرمت کوتاه
/// «HH:mm» (که پیش‌تر دسکتاپ برای ذخیرهٔ شیفت می‌فرستاد) با JsonException رد
/// می‌شود → شکست ModelBinding → پیام عمومی ۴۰۰ «اطلاعات ارسالی نامعتبر است».
///
/// ═══ راهکار دولایه ═══
/// ۱) دسکتاپ اکنون «HH:mm:ss» می‌فرستد (اصلاح هم‌زمان در ShiftsViewModel)؛
/// ۲) این مبدل به‌صورت سراسری در AddJsonOptions ثبت می‌شود تا «هر» فراخوان
///    قدیمی/ثالث که هنوز فرمت کوتاه می‌فرستد هم بی‌صدا پذیرفته شود — قاعدهٔ
///    مالک: «خطاهایی که برطرف می‌کنی را در جاهای دیگر هم برطرف کن». این مبدل
///    سه قالب را می‌پذیرد: HH:mm · HH:mm:ss · HH:mm:ss.fffffff.
///
/// ═══ علامت‌گذاری برای خروجی ═══
/// سریال‌سازی همان رفتار بومی (HH:mm:ss) حفظ می‌شود تا کلاینت‌های موجود
/// (مثل TimeOnly.TryParse در دسکتاپ) رفتار قبلی را ببینند.
/// </remarks>
public sealed class TimeOnlyLenientJsonConverter : JsonConverter<TimeOnly>
{
    /// <summary>
    /// خواندن — سه قالب ساعت پذیرفته می‌شود؛ هر قالب دیگر همان خطای
    /// استاندارد JsonException را می‌دهد (پیام ۴۰۰ سرور ریشه‌یابی‌پذیر می‌ماند).
    /// </summary>
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("مقدار ساعت تهی است");

        // تلاش با قالب‌های مجاز — از کامل به کوتاه (هر دو حساس به فرهنگ ثابت)
        var trimmed = raw.Trim();
        if (TimeOnly.TryParseExact(trimmed, "HH:mm:ss.fffffff", out var full) ||
            TimeOnly.TryParseExact(trimmed, "HH:mm:ss", out full) ||
            TimeOnly.TryParseExact(trimmed, "H:mm:ss", out full) ||
            TimeOnly.TryParseExact(trimmed, "HH:mm", out full) ||
            TimeOnly.TryParseExact(trimmed, "H:mm", out full))
        {
            return full;
        }

        throw new JsonException($"قالب ساعت نامعتبر است: «{raw}» — قالب‌های مجاز: HH:mm یا HH:mm:ss");
    }

    /// <summary>نوشتن — همان قالب بومی «HH:mm:ss» (سازگار با مصرف‌کنندگان فعلی)</summary>
    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("HH:mm:ss"));
}

/// <summary>
/// مبدل JSON «سهل‌گیر» برای DateOnly — هم‌الگوی TimeOnlyLenientJsonConverter.
/// </summary>
/// <remarks>
/// DateOnly بومی فقط «yyyy-MM-dd» می‌پذیرد؛ این مبدل همان قالب را نگه می‌دارد
/// و فقط پیام خطای فارسیِ شفاف اضافه می‌کند (تاریخ شمسی مثل «1404/07/21»
/// نباید بی‌توضیح رد شود — پیام روشن می‌کند قالب درست چیست).
/// </remarks>
public sealed class DateOnlyLenientJsonConverter : JsonConverter<DateOnly>
{
    /// <summary>خواندن — فقط قالب استاندارد ISO؛ خطا با پیام فارسی روشن</summary>
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("مقدار تاریخ تهی است");

        if (DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", out var parsed))
            return parsed;

        throw new JsonException($"قالب تاریخ نامعتبر است: «{raw}» — قالب مجاز: yyyy-MM-dd (میلادی)");
    }

    /// <summary>نوشتن — قالب استاندارد ISO «yyyy-MM-dd»</summary>
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
}
