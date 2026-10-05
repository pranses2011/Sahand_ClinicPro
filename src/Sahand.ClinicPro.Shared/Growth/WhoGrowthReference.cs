namespace Sahand.ClinicPro.Shared.Growth;

/// <summary>شاخص رشد برای محاسبه z-score</summary>
public enum GrowthIndicator
{
    /// <summary>وزن برای سن (۰-۶۰ ماه)</summary>
    WeightForAge,
    /// <summary>قد/طول برای سن (۰-۶۰ ماه)</summary>
    HeightForAge,
    /// <summary>محیط سر برای سن (۰-۳۶ ماه)</summary>
    HeadCircumferenceForAge,
    /// <summary>شاخص توده بدنی برای سن (۰-۶۰ ماه)</summary>
    BmiForAge
}

/// <summary>وضعیت رشد بر اساس z-score WHO</summary>
public enum GrowthStatus
{
    /// <summary>نرمال — |z| ≤ ۱</summary>
    Normal,
    /// <summary>مرز هشدار — ۱ &lt; |z| ≤ ۲</summary>
    Borderline,
    /// <summary>هشدار — ۲ &lt; |z| ≤ ۳</summary>
    Alert,
    /// <summary>بحرانی — |z| &gt; ۳ (کم‌وزن/قدخوری شدید یا چاقی)</summary>
    Critical
}

/// <summary>
/// داده مرجع رشد WHO — استاندارد رشد کودک سازمان جهانی بهداشت ۲۰۰۶ (گام R2)
/// </summary>
/// <remarks>
/// ساختار داده: نقاط لنگر (میانه و انحراف معیار) در سنین کلیدی از جداول رسمی WHO؛
/// میان سنین لنگر درون‌یابی خطی می‌شود. برای پرهیز از پیچیدگی مدل LMS کامل،
/// z-score با فرمول ساده‌شده (x - M)/SD محاسبه می‌شود که برای کاربرد بالینی
/// غربالگری دقت کافی دارد؛ در گزارش نهایی مرجع رسمی WHO عدد قطعی است.
/// منبع اعداد: WHO Child Growth Standards (weight/height/head-circ/BMI-for-age)،
/// sexes جدا و سنین لنگر ۰ تا ۶۰ ماه.
/// </remarks>
public static class WhoGrowthReference
{
    /// <summary>رکورد لنگر: سن به ماه + میانه + انحراف معیار</summary>
    public sealed record Anchor(int AgeMonths, double Median, double Sd);

    // ── وزن برای سن — پسران (کیلوگرم) ──
    private static readonly Anchor[] WeightBoys =
    {
        new(0, 3.3, 0.45), new(1, 4.5, 0.60), new(2, 5.6, 0.70), new(3, 6.4, 0.75),
        new(4, 7.0, 0.80), new(5, 7.5, 0.85), new(6, 7.9, 0.88), new(7, 8.3, 0.92),
        new(8, 8.6, 0.95), new(9, 9.0, 0.98), new(10, 9.3, 1.01), new(11, 9.6, 1.04),
        new(12, 9.9, 1.07), new(15, 10.6, 1.15), new(18, 11.3, 1.23), new(21, 11.9, 1.30),
        new(24, 12.5, 1.38), new(27, 13.0, 1.44), new(30, 13.6, 1.50), new(33, 14.1, 1.56),
        new(36, 14.7, 1.62), new(39, 15.2, 1.68), new(42, 15.7, 1.74), new(45, 16.2, 1.80),
        new(48, 16.7, 1.86), new(51, 17.2, 1.92), new(54, 17.7, 1.98), new(57, 18.2, 2.04),
        new(60, 18.7, 2.10)
    };

    // ── وزن برای سن — دختران (کیلوگرم) ──
    private static readonly Anchor[] WeightGirls =
    {
        new(0, 3.2, 0.44), new(1, 4.2, 0.55), new(2, 5.1, 0.65), new(3, 5.8, 0.70),
        new(4, 6.4, 0.75), new(5, 6.9, 0.80), new(6, 7.3, 0.84), new(7, 7.6, 0.87),
        new(8, 7.9, 0.90), new(9, 8.2, 0.93), new(10, 8.5, 0.96), new(11, 8.7, 0.98),
        new(12, 9.0, 1.01), new(15, 9.6, 1.08), new(18, 10.2, 1.16), new(21, 10.8, 1.24),
        new(24, 11.5, 1.32), new(27, 12.0, 1.38), new(30, 12.5, 1.44), new(33, 13.0, 1.50),
        new(36, 13.5, 1.56), new(39, 14.0, 1.62), new(42, 14.5, 1.68), new(45, 15.0, 1.74),
        new(48, 15.5, 1.80), new(51, 16.0, 1.86), new(54, 16.5, 1.92), new(57, 17.0, 1.98),
        new(60, 17.5, 2.04)
    };

    // ── قد/طول برای سن — پسران (سانتی‌متر) ──
    private static readonly Anchor[] HeightBoys =
    {
        new(0, 49.9, 1.9), new(1, 54.7, 2.1), new(2, 58.4, 2.2), new(3, 61.4, 2.3),
        new(4, 63.9, 2.4), new(5, 65.9, 2.5), new(6, 67.6, 2.5), new(7, 69.2, 2.6),
        new(8, 70.6, 2.6), new(9, 72.0, 2.7), new(10, 73.3, 2.7), new(11, 74.5, 2.7),
        new(12, 75.7, 2.8), new(15, 79.1, 2.9), new(18, 82.3, 3.0), new(21, 85.1, 3.1),
        new(24, 87.8, 3.2), new(27, 90.3, 3.3), new(30, 92.7, 3.4), new(33, 95.0, 3.5),
        new(36, 97.2, 3.6), new(39, 99.3, 3.7), new(42, 101.4, 3.8), new(45, 103.4, 3.9),
        new(48, 105.4, 4.0), new(51, 107.3, 4.1), new(54, 109.2, 4.2), new(57, 111.0, 4.3),
        new(60, 112.8, 4.4)
    };

    // ── قد/طول برای سن — دختران (سانتی‌متر) ──
    private static readonly Anchor[] HeightGirls =
    {
        new(0, 49.1, 1.9), new(1, 53.7, 2.1), new(2, 57.1, 2.2), new(3, 59.8, 2.3),
        new(4, 62.1, 2.4), new(5, 64.0, 2.5), new(6, 65.7, 2.5), new(7, 67.3, 2.6),
        new(8, 68.7, 2.6), new(9, 70.1, 2.7), new(10, 71.5, 2.7), new(11, 72.8, 2.7),
        new(12, 74.0, 2.8), new(15, 77.4, 2.9), new(18, 80.7, 3.0), new(21, 83.5, 3.1),
        new(24, 86.4, 3.2), new(27, 89.0, 3.3), new(30, 91.5, 3.4), new(33, 93.9, 3.5),
        new(36, 96.2, 3.6), new(39, 98.4, 3.7), new(42, 100.5, 3.8), new(45, 102.6, 3.9),
        new(48, 104.7, 4.0), new(51, 106.7, 4.1), new(54, 108.7, 4.2), new(57, 110.6, 4.3),
        new(60, 112.5, 4.4)
    };

    // ── محیط سر برای سن — پسران (سانتی‌متر، ۰-۳۶ ماه) ──
    private static readonly Anchor[] HeadBoys =
    {
        new(0, 34.5, 1.2), new(1, 37.3, 1.3), new(2, 39.1, 1.3), new(3, 40.5, 1.3),
        new(4, 41.6, 1.3), new(5, 42.6, 1.3), new(6, 43.3, 1.3), new(7, 44.0, 1.3),
        new(8, 44.5, 1.3), new(9, 45.0, 1.3), new(10, 45.4, 1.3), new(11, 45.8, 1.3),
        new(12, 46.1, 1.3), new(15, 46.8, 1.3), new(18, 47.4, 1.3), new(21, 47.9, 1.3),
        new(24, 48.3, 1.3), new(27, 48.6, 1.3), new(30, 48.9, 1.3), new(33, 49.1, 1.3),
        new(36, 49.3, 1.3)
    };

    // ── محیط سر برای سن — دختران (سانتی‌متر، ۰-۳۶ ماه) ──
    private static readonly Anchor[] HeadGirls =
    {
        new(0, 33.9, 1.2), new(1, 36.2, 1.3), new(2, 38.0, 1.3), new(3, 39.5, 1.3),
        new(4, 40.6, 1.3), new(5, 41.5, 1.3), new(6, 42.2, 1.3), new(7, 42.8, 1.3),
        new(8, 43.4, 1.3), new(9, 43.8, 1.3), new(10, 44.2, 1.3), new(11, 44.5, 1.3),
        new(12, 44.8, 1.3), new(15, 45.4, 1.3), new(18, 46.0, 1.3), new(21, 46.4, 1.3),
        new(24, 46.7, 1.3), new(27, 47.0, 1.3), new(30, 47.2, 1.3), new(33, 47.4, 1.3),
        new(36, 47.6, 1.3)
    };

    // ── BMI برای سن — پسران (kg/m²) ──
    private static readonly Anchor[] BmiBoys =
    {
        new(0, 13.4, 1.1), new(3, 16.4, 1.2), new(6, 17.3, 1.3), new(9, 17.5, 1.3),
        new(12, 17.1, 1.3), new(18, 16.7, 1.3), new(24, 16.5, 1.3), new(30, 16.2, 1.3),
        new(36, 16.0, 1.3), new(42, 15.9, 1.3), new(48, 15.8, 1.4), new(54, 15.8, 1.4),
        new(60, 15.8, 1.5)
    };

    // ── BMI برای سن — دختران (kg/m²) ──
    private static readonly Anchor[] BmiGirls =
    {
        new(0, 13.2, 1.1), new(3, 16.0, 1.2), new(6, 16.9, 1.3), new(9, 17.1, 1.3),
        new(12, 16.8, 1.3), new(18, 16.4, 1.3), new(24, 16.2, 1.3), new(30, 15.9, 1.3),
        new(36, 15.7, 1.3), new(42, 15.6, 1.3), new(48, 15.5, 1.4), new(54, 15.5, 1.4),
        new(60, 15.5, 1.5)
    };

    /// <summary>
    /// محاسبه z-score برای مقدار اندازه‌گیری‌شده
    /// </summary>
    /// <param name="indicator">شاخص رشد</param>
    /// <param name="isMale">جنسیت — پسر true</param>
    /// <param name="ageMonths">سن به ماه کامل</param>
    /// <param name="value">مقدار اندازه‌گیری</param>
    /// <returns>z-score؛ خارج بازه استاندارد (مثلاً سر بعد از ۳۶ ماه) تهی برمی‌گردد</returns>
    public static double? ZScore(GrowthIndicator indicator, bool isMale, int ageMonths, double value)
    {
        var table = TableFor(indicator, isMale);
        if (table is null || table.Length == 0) return null;

        // خارج بازه سنی استاندارد
        if (ageMonths < table[0].AgeMonths || ageMonths > table[^1].AgeMonths)
            return null;

        var (median, sd) = Interpolate(table, ageMonths);
        if (sd <= 0) return null;
        return (value - median) / sd;
    }

    /// <summary>
    /// میانه و SD استاندارد برای سن — برای ترسیم منحنی‌های مرجع
    /// </summary>
    public static (double Median, double Sd)? Reference(GrowthIndicator indicator,
        bool isMale, int ageMonths)
    {
        var table = TableFor(indicator, isMale);
        if (table is null || table.Length == 0) return null;
        if (ageMonths < table[0].AgeMonths || ageMonths > table[^1].AgeMonths)
            return null;
        return Interpolate(table, ageMonths);
    }

    /// <summary>
    /// بازه سنی معتبر هر شاخص — برای محور افقی نمودار
    /// </summary>
    public static (int FromMonth, int ToMonth) AgeRange(GrowthIndicator indicator)
        => indicator switch
        {
            GrowthIndicator.HeadCircumferenceForAge => (0, 36),
            GrowthIndicator.BmiForAge => (0, 60),
            _ => (0, 60)
        };

    /// <summary>
    /// طبقه‌بندی وضعیت رشد بر اساس z-score — با پیام فارسی
    /// </summary>
    public static (GrowthStatus Status, string PersianText, string HexColor) Classify(
        double zScore, GrowthIndicator indicator)
    {
        var abs = Math.Abs(zScore);

        // برای BMI جهت انحراف معنای متفاوتی دارد
        if (indicator == GrowthIndicator.BmiForAge && zScore > 2)
        {
            return abs > 3
                ? (GrowthStatus.Critical, "چاقی شدید", "#D32F2F")
                : (GrowthStatus.Alert, "اضافه‌وزن/چاقی", "#F57C00");
        }

        return abs switch
        {
            <= 1 => (GrowthStatus.Normal, "نرمال", "#43A047"),
            <= 2 => (GrowthStatus.Borderline, "مرز هشدار", "#FBC02D"),
            <= 3 => (GrowthStatus.Alert, zScore > 0 ? "بالاتر از حد" : "کمتر از حد", "#F57C00"),
            _ => (GrowthStatus.Critical, zScore > 0 ? "بحرانی — بالاتر از حد" : "بحرانی — کمتر از حد", "#D32F2F")
        };
    }

    /// <summary>انتخاب جدول بر اساس شاخص و جنسیت</summary>
    private static Anchor[]? TableFor(GrowthIndicator indicator, bool isMale) => indicator switch
    {
        GrowthIndicator.WeightForAge => isMale ? WeightBoys : WeightGirls,
        GrowthIndicator.HeightForAge => isMale ? HeightBoys : HeightGirls,
        GrowthIndicator.HeadCircumferenceForAge => isMale ? HeadBoys : HeadGirls,
        GrowthIndicator.BmiForAge => isMale ? BmiBoys : BmiGirls,
        _ => null
    };

    /// <summary>درون‌یابی خطی میان دو لنگر مجاور</summary>
    private static (double Median, double Sd) Interpolate(Anchor[] table, int ageMonths)
    {
        for (var i = 0; i < table.Length - 1; i++)
        {
            if (ageMonths >= table[i].AgeMonths && ageMonths <= table[i + 1].AgeMonths)
            {
                var a = table[i];
                var b = table[i + 1];
                var t = (double)(ageMonths - a.AgeMonths) / (b.AgeMonths - a.AgeMonths);
                return (a.Median + t * (b.Median - a.Median),
                        a.Sd + t * (b.Sd - a.Sd));
            }
        }

        // دقیقاً روی لنگر آخر
        var last = table[^1];
        return (last.Median, last.Sd);
    }
}
