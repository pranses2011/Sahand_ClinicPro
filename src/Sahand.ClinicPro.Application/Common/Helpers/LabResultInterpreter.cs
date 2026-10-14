using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>نتیجهٔ تفسیر یک نتیجهٔ آزمایش — فلگ استاندارد آزمایشگاهی</summary>
public enum LabResultFlag
{
    /// <summary>طبیعی — داخل بازهٔ مرجع بیمار</summary>
    Normal,

    /// <summary>پایین‌تر از حد طبیعی (Low)</summary>
    Low,

    /// <summary>بالاتر از حد طبیعی (High)</summary>
    High,

    /// <summary>بحرانی پایین (Critical Low) — هشدار فوری پزشک (F-29)</summary>
    CriticalLow,

    /// <summary>بحرانی بالا (Critical High) — هشدار فوری پزشک (F-29)</summary>
    CriticalHigh,

    /// <summary>قابل تفسیر نیست — تست بازهٔ مرجع ثبت‌شده ندارد یا نتیجه متنی است</summary>
    Unknown
}

/// <summary>
/// موتور تفسیر خودکار نتیجهٔ آزمایش بر اساس مشخصات بیمار — چرخهٔ ۶۹
/// (فاز ۳ / F-28 «محدودهٔ مرجع سنی/جنسیتی» + F-29 «مقادیر بحرانی»)
///
/// ─────────────────────────────────────────────────────────────────────
/// چرا این کلاس لازم بود؟ هندلر ثبت نتیجه از روز اول «تفسیر» نداشت:
/// IsAbnormal فقط برای اعداد منفی (عملاً هرگز) true می‌شد و AbnormalFlag
/// همیشه «H» می‌زد — یعنی همهٔ نتایج عددی «بالا» پرچم می‌خوردند و هیچ‌کدام
/// غیرطبیعی شمرده نمی‌شدند! این موتور بازهٔ مرجع «مخصوص بیمار» را از سن و
/// جنسیت محاسبه می‌کند (کودک &lt; سقف سن کودک؛ بزرگسال بر حسب جنسیت) و فلگ
/// پنج‌حالته (N/L/H/LL/HH) به‌علاوهٔ متن بازهٔ نمایشی برمی‌گرداند.
///
/// قواعد (به‌ترتیب اولویت):
/// ۱) نتیجهٔ تهی/متنی بدون عدد ← Unknown (تفسیر نمی‌شود؛ نتیجهٔ متنی مثل
///    «منفی» یا «دیده نشد» معیار عددی ندارد).
/// ۲) تست بازهٔ عددی ثبت‌شده ندارد ← Unknown.
/// ۳) مقدار بحرانی (≤ CriticalLow یا ≥ CriticalHigh) ← CriticalLow/CriticalHigh.
/// ۴) خارج از بازهٔ مرجع ← Low/High.
/// ۵) در غیر این صورت ← Normal.
/// </summary>
public static class LabResultInterpreter
{
    /// <summary>
    /// تفسیر نتیجهٔ عددی یک قلم بر اساس مشخصات بیمار و بازه‌های تست.
    /// </summary>
    /// <param name="test">کاتالوگ تست با بازه‌های مرجع و حدود بحرانی</param>
    /// <param name="resultNumeric">نتیجهٔ عددی قلم — تهی یعنی نتیجهٔ متنی</param>
    /// <param name="patient">بیمار — برای سن و جنسیت؛ تهی یعنی بازهٔ عمومی مردان</param>
    public static LabResultFlag Interpret(LabTest test, decimal? resultNumeric, Patient? patient)
    {
        // ۱) بدون عدد، تفسیر عددی ممکن نیست (نتیجهٔ کیفی مثل «منفی»)
        if (resultNumeric is null)
            return LabResultFlag.Unknown;

        // ۲) بدون هیچ بازهٔ ثبت‌شده، قضاوت دربارهٔ طبیعی‌بودن بی‌پایه است
        var (min, max) = GetPatientRange(test, patient);
        if (min is null && max is null)
            return LabResultFlag.Unknown;

        // ۳) اولویت اول — حدود بحرانی: خطر بالینی مستقیم (F-29)
        if (test.CriticalLow.HasValue && resultNumeric <= test.CriticalLow)
            return LabResultFlag.CriticalLow;
        if (test.CriticalHigh.HasValue && resultNumeric >= test.CriticalHigh)
            return LabResultFlag.CriticalHigh;

        // ۴) خارج از بازهٔ مرجع مخصوص بیمار
        if (min.HasValue && resultNumeric < min)
            return LabResultFlag.Low;
        if (max.HasValue && resultNumeric > max)
            return LabResultFlag.High;

        // ۵) داخل بازه
        return LabResultFlag.Normal;
    }

    /// <summary>
    /// بازهٔ مرجع مخصوص این بیمار — کودک (&lt; سقف سن کودک تست) بازهٔ کودک
    /// می‌گیرد؛ بزرگسال بر حسب جنسیت؛ بازهٔ کودکِ ثبت‌نشده به بازهٔ بزرگسال
    /// همان جنسیت برمی‌گردد تا کودکِ بدون بازهٔ اختصاصی بی‌تفسیر نماند.
    /// </summary>
    public static (decimal? Min, decimal? Max) GetPatientRange(LabTest test, Patient? patient)
    {
        var isChild = patient?.BirthDate is { } birth &&
                      test.ChildAgeMaxYears.HasValue &&
                      AgeAt(birth) < test.ChildAgeMaxYears.Value;

        if (isChild && (test.RefMinChild.HasValue || test.RefMaxChild.HasValue))
            return (test.RefMinChild, test.RefMaxChild);

        // جنسیت زن (مقدار ۲ در قرارداد معمول Person) بازهٔ زنانه؛ هر چیز
        // دیگر (مرد/نامشخص) بازهٔ مردانه — بازهٔ تهی به بازهٔ مقابل سقوط می‌کند
        var female = patient?.Gender == Gender.Female;
        var (min, max) = female
            ? (test.RefMinAdultFemale, test.RefMaxAdultFemale)
            : (test.RefMinAdultMale, test.RefMaxAdultMale);

        // سقوط نهایی — بازهٔ جنسیت مقابل ثبت شده باشد استفاده می‌شود
        if (min is null && max is null)
            (min, max) = female
                ? (test.RefMinAdultMale, test.RefMaxAdultMale)
                : (test.RefMinAdultFemale, test.RefMaxAdultFemale);

        return (min, max);
    }

    /// <summary>
    /// متن نمایشی بازهٔ مرجع مخصوص بیمار — «۱۳ تا ۱۷ mg/dL»؛ برای فرم ثبت
    /// نتیجه (نمایش رنگی حد طبیعی این بیمار) و برگهٔ چاپی نتیجه.
    /// ═══ چرخهٔ ۷۵ (گزارش مالک: «برای همه بیماران میگه حد طبیعی برای این
    /// بیمار منفی میباشد — به جای عدد منفی مینویسه») ═══
    /// آزمایش‌های کیفی (کشت/سرولوژی/اسمیر) عدد ندارند و «منفی» یعنی نتیجهٔ
    /// طبیعیِ همان آزمایش — نه جای‌گزینِ عددِ گم‌شده! پیش‌تر فقط «منفی» چاپ
    /// می‌شد و به نظر خطای نمایش عدد می‌رسید؛ اکنون با برچسب شفاف
    /// «کیفی — نتیجهٔ طبیعی: منفی» نمایش می‌یابد تا معنای بالینی‌اش روشن باشد.
    /// آزمایش‌های کمی همچنان «۱۳٫۵ تا ۱۷٫۵ g/dL» می‌گیرند.
    /// </summary>
    public static string RangeDisplayText(LabTest test, Patient? patient)
    {
        var (min, max) = GetPatientRange(test, patient);
        if (min is null && max is null)
        {
            var text = test.NormalRangeMale ?? test.NormalRangeFemale ?? "—";
            // آزمایش کیفی — «منفی/مثبت/گزارش» یعنی ماهیت نتیجهٔ طبیعی؛ با
            // برچسب روشن نمایش داده می‌شود تا با «عددِ درست» اشتباه نشود
            if (text is "منفی" or "مثبت" or "گزارش")
                return $"کیفی — نتیجهٔ طبیعی: {text}";
            return text;
        }
        var unit = string.IsNullOrWhiteSpace(test.Unit) ? "" : $" {test.Unit}";
        if (min is null) return $"تا {max}{unit}";
        if (max is null) return $"از {min}{unit}";
        return $"{min} تا {max}{unit}";
    }

    /// <summary>سن به سال تمام‌شده در امروز — برای تصمیم کودک/بزرگسال</summary>
    private static int AgeAt(DateOnly birth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birth.Year;
        if (birth > today.AddYears(-age)) age--;
        return age;
    }
}
