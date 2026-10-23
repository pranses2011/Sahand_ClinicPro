namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// ماشین‌حساب حقوق و دستمزد — قواعد ایران ۱۴۰۴ (گام R3 + حقوق درصدی چرخه نهم)
/// </summary>
/// <remarks>
/// مفروضات قانونی (قابل تنظیم با ثابت‌های این کلاس):
/// ۱. ساعت کار ماهانه قانونی: ۱۹۲ ساعت (۴۴ ساعت در هفته × ۴.۳۴)
/// ۲. نرخ اضافه‌کاری: ۱/۴ برابر نرخ ساعت عادی (ماده ۵۹ قانون کار)
/// ۳. بیمه سهم کارمند: در صورت انتخاب شمول بیمه برای پرسنل، ۷٪ مزایای مشمول (ماده ۲۸ قانون تأمین اجتماعی)
/// ۴. پلکان ماهانه مالیات حقوق ۱۴۰۴ (ماده ۸۴ ق.ب. ۱۴۰۴ — ارقام ریال):
///    تا ۲۴۰م ریال معاف | ۲۴۰ تا ۳۶۰م: ۱۰٪ | ۳۶۰ تا ۶۰۰م: ۱۵٪ | بالاتر: ۲۰٪
///    (پلکان نرخی تجمعی — نه نرخ مؤثر بر کل مبلغ)
/// ۵. کسور بیمه پیش از محاسبه مالیات از مبنا کسر می‌شود
/// ۶. حقوق درصدی (کارکرد/پورسانت): فقط از «سود خدمات انجام‌شده توسط خودِ پرسنل»
///    محاسبه می‌شود — نه از کل فروش و نه از کل دریافتی؛ و چون جزو حقوق
///    و مزایای مشمول است، در کل مزایای بیمه‌خواه/مالیات‌خواه لحاظ می‌شود.
///    سود منفی → حقوق درصدی صفر (ضرر دوره بر عهده کارفرماست نه پرسنل)
/// </remarks>
public static class PayrollCalculator
{
    /// <summary>ساعت کار قانونی ماهانه</summary>
    public const decimal LegalMonthlyHours = 192m;

    /// <summary>ضریب اضافه‌کاری بر نرخ ساعت عادی</summary>
    public const decimal OvertimeMultiplier = 1.4m;

    /// <summary>نرخ بیمه سهم کارمند (اعشار)</summary>
    public const decimal EmployeeInsuranceRate = 0.07m;

    /// <summary>سقف معافیت ماهانه مالیات حقوق (ریال)</summary>
    public const decimal MonthlyTaxExemption = 240_000_000m;

    /// <summary>سقف پله اول (۱/۵ برابر معافیت — ریال)</summary>
    public const decimal FirstBracketCeiling = 360_000_000m;

    /// <summary>سقف پله دوم (۲/۵ برابر معافیت — ریال)</summary>
    public const decimal SecondBracketCeiling = 600_000_000m;

    /// <summary>نرخ پله اول تا دوم (اعشار)</summary>
    public const decimal FirstBracketRate = 0.10m;

    /// <summary>نرخ پله دوم تا سوم</summary>
    public const decimal SecondBracketRate = 0.15m;

    /// <summary>نرخ پله سوم به بالا</summary>
    public const decimal ThirdBracketRate = 0.20m;

    /// <summary>ورودی محاسبه حقوق ماهانه</summary>
    public sealed record PayrollInput(
        decimal BaseSalary,
        decimal OvertimeHours = 0m,
        decimal Bonus = 0m,
        decimal OtherDeductions = 0m,
        decimal CommissionPercent = 0m,
        decimal ServiceProfit = 0m,
        bool IsInsured = false);

    /// <summary>خروجی محاسبه — همه مبالغ ریال</summary>
    public sealed record PayrollResult(
        decimal OvertimePay,
        decimal ServiceCommission,
        decimal GrossEarnings,
        decimal Insurance,
        decimal TaxableBase,
        decimal Tax,
        decimal TotalDeductions,
        decimal NetAmount);

    /// <summary>
    /// محاسبه کامل حقوق ماهانه بر اساس قواعد ۱۴۰۴ + حقوق درصدی از سود خدمات
    /// </summary>
    public static PayrollResult Calculate(PayrollInput input)
    {
        // نرخ هر ساعت عادی → اضافه‌کاری ۱/۴ برابر
        var hourlyRate = input.BaseSalary / LegalMonthlyHours;
        var overtimePay = Math.Round(hourlyRate * OvertimeMultiplier * input.OvertimeHours, 0);

        // حقوق درصدی = سود خدمات انجامی خودِ پرسنل × درصد — فقط از سود (نه کل فروش)؛
        // سود منفی یا درصد صفر → بدون حقوق درصدی
        var serviceCommission = Math.Round(
            Math.Max(0m, input.ServiceProfit) * input.CommissionPercent / 100m, 0);

        // کل مزایای مشمول بیمه و مالیات (پورسانت هم جزو مزایای مشمول است)
        var gross = input.BaseSalary + overtimePay + input.Bonus + serviceCommission;

        // بیمه سهم کارمند فقط برای پرسنلی کسر می‌شود که در پرونده‌اش بیمه فعال است.
        var insurance = input.IsInsured
            ? Math.Round(gross * EmployeeInsuranceRate, 0)
            : 0m;

        // مبنای مشمول مالیات پس از کسر بیمه
        var taxableBase = gross - insurance;

        // مالیات پلکانی تجمعی
        var tax = ProgressiveTax(taxableBase);

        var totalDeductions = insurance + tax + input.OtherDeductions;
        var net = gross - totalDeductions;

        return new PayrollResult(overtimePay, serviceCommission, gross, insurance, taxableBase,
            tax, totalDeductions, net);
    }

    /// <summary>
    /// مالیات پلکانی تجمعی — هر پله فقط بر مازاد پله قبل
    /// </summary>
    public static decimal ProgressiveTax(decimal taxableBase)
    {
        if (taxableBase <= MonthlyTaxExemption) return 0;

        // پله اول: از سقف معافیت تا ۱.۵ برابر
        var firstBand = Math.Min(taxableBase, FirstBracketCeiling) - MonthlyTaxExemption;
        var tax = Math.Max(0, firstBand) * FirstBracketRate;

        if (taxableBase <= FirstBracketCeiling) return Math.Round(tax, 0);

        // پله دوم: از ۱.۵ تا ۲.۵ برابر
        var secondBand = Math.Min(taxableBase, SecondBracketCeiling) - FirstBracketCeiling;
        tax += secondBand * SecondBracketRate;

        if (taxableBase <= SecondBracketCeiling) return Math.Round(tax, 0);

        // پله سوم: مازاد بر ۲.۵ برابر
        tax += (taxableBase - SecondBracketCeiling) * ThirdBracketRate;
        return Math.Round(tax, 0);
    }
}
