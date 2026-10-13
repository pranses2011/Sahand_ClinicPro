namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// ═══ کاتالوگ برچسب فارسی کارت‌های خلاصهٔ گزارش — چرخهٔ ۷۰ ═══
/// گزارش مالک: «عنوان‌های گزارش‌ها همگی انگلیسی‌اند که باید فارسی شوند».
/// ریشه: سرور کلیدهای خلاصه را انگلیسی می‌فرستد (total، paid، count، mrn و…) —
/// کارت‌های خلاصه همان کلید را عیناً نشان می‌دادند («total: ۵۰۰٬۰۰۰»).
/// این کاتالوگ در «لحظهٔ نمایش» کلید را به فارسی برمی‌گرداند — بدون دست‌کاری
/// سرور و سازگار با ردیف‌های آینده؛ کلید ناشناخته با فاصله‌گذاری بین کلمات
/// نشان داده می‌شود (هرگز حذف نمی‌شود تا داده‌ای گم نشود).
/// پوشش: ~۹۰ کلید واقعی استخراج‌شده از همهٔ ۵۵+ گزارش پنج کنترلر گزارش.
/// </summary>
public static class ReportLabelCatalog
{
    /// <summary>نگاشت کلید انگلیسی سرور → برچسب فارسی خوانا</summary>
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        // ─── مبالغ ───
        ["total"] = "جمع کل", ["paid"] = "وصول‌شده", ["unpaid"] = "پرداخت‌نشده",
        ["balance"] = "مانده", ["outstanding"] = "مطالبات باقیمانده",
        ["revenue"] = "درآمد", ["totalRevenue"] = "جمع درآمد",
        ["gross"] = "ناخالص", ["net"] = "خالص", ["margin"] = "حاشیه سود",
        ["expenses"] = "هزینه‌ها", ["refunds"] = "بازپرداخت‌ها",
        ["claimed"] = "مطالبه‌شده", ["declared"] = "اظهارشده",
        ["dailyAvg"] = "میانگین روزانه", ["monthAvg"] = "میانگین ماهانه",
        ["overallAvg"] = "میانگین کل", ["avgPerDay"] = "میانگین در روز",
        ["totalThisYear"] = "جمع امسال", ["totalLastYear"] = "جمع سال گذشته",
        // ─── شمارش‌ها ───
        ["count"] = "تعداد", ["days"] = "روزها",
        ["invoices"] = "فاکتورها", ["payments"] = "پرداخت‌ها",
        ["patients"] = "بیماران", ["newPatients"] = "بیماران جدید",
        ["visits"] = "ویزیت‌ها", ["firstVisit"] = "ویزیت نخست",
        ["appointments"] = "نوبت‌ها", ["cancelled"] = "لغوشده",
        ["completed"] = "تکمیل‌شده", ["noShow"] = "بدون مراجعه",
        ["doctors"] = "پزشکان", ["staff"] = "پرسنل", ["personnel"] = "پرسنل",
        ["branches"] = "شعب", ["insurances"] = "بیمه‌گران", ["insurers"] = "بیمه‌گران",
        ["insured"] = "بیمه‌شده‌ها", ["debtors"] = "بدهکاران",
        ["services"] = "خدمات", ["medications"] = "داروها", ["items"] = "اقلام",
        ["orders"] = "درخواست‌ها", ["tests"] = "آزمون‌ها", ["dispenses"] = "تجویزها",
        ["diagnoses"] = "تشخیص‌ها", ["expiring"] = "در آستانهٔ انقضا",
        ["lowStock"] = "موجودی کم", ["claims"] = "مطالبات بیمه",
        ["supplementary"] = "بیمه تکمیلی", ["vip"] = "بیماران ویژه",
        ["windowMonths"] = "پنجرهٔ ماه‌ها", ["monthsAnalyzed"] = "ماه‌های تحلیل‌شده",
        ["months"] = "ماه‌ها",
        // ─── بیمار/پرسنل ───
        ["patient"] = "بیمار", ["mrn"] = "شماره پرونده",
        ["female"] = "زنان", ["male"] = "مردان",
        ["withBirthDate"] = "با تاریخ تولد",
        ["closingBalance"] = "مانده پایانی",
        // ─── عملکرد و روند ───
        ["peak"] = "اوج", ["busiest"] = "پرتردد", ["busiestDay"] = "پرترددترین روز",
        ["bestMonth"] = "بهترین ماه", ["oldest"] = "قدیمی‌ترین",
        ["trend"] = "روند", ["trendPercent"] = "درصد روند", ["trendText"] = "متن روند",
        ["avgGrowth"] = "میانگین رشد", ["lastMonthGrowth"] = "رشد ماه گذشته",
        ["yoyGrowth"] = "رشد سال‌به‌سال", ["firstRatio"] = "نسبت ویزیت نخست",
        ["forecastNextMonth"] = "پیش‌بینی ماه آینده",
        ["confidenceLow"] = "کران پایین اطمینان", ["confidenceHigh"] = "کران بالای اطمینان",
        // ─── RFM و رده‌بندی ───
        ["champions"] = "قهرمانان", ["atRisk"] = "در معرض خطر",
        // ─── انبار و روش‌ها ───
        ["totalQty"] = "جمع مقدار", ["methods"] = "روش‌های پرداخت",
        ["cash"] = "نقدی", ["card"] = "کارتخوان",
        // ─── وضعیت‌ها ───
        ["status"] = "وضعیت", ["type"] = "نوع", ["period"] = "دوره",
        ["date"] = "تاریخ", ["top"] = "برترین‌ها", ["hint"] = "راهنما",
        ["label"] = "برچسب",
    };

    /// <summary>
    /// برچسب فارسی برای کلید خلاصه — کلید از پیش فارسی باشد بدون تغییر
    /// برمی‌گردد؛ ناشناخته با فاصله‌گذاری camelCase («totalRevenue» → «total Revenue»).
    /// </summary>
    public static string Persianize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "—";
        key = key.Trim();
        if (Labels.TryGetValue(key, out var fa))
            return fa;
        if (key.Any(c => c is >= '\u0600' and <= '\u06FF'))
            return key; // از قبل فارسی است
        var spaced = string.Concat(key.Select((c, i) =>
            i > 0 && char.IsUpper(c) && !char.IsUpper(key[i - 1]) ? " " + c : c.ToString()));
        return spaced.Trim();
    }
}
