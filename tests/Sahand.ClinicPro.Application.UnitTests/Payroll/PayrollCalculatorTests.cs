using FluentAssertions;
using Sahand.ClinicPro.Application.Common.Helpers;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Payroll;

/// <summary>
/// تست‌های ماشین‌حساب حقوق ۱۴۰۴ — گام R3
/// بیمه ۷٪، اضافه‌کاری ۱/۴×، پلکان مالیات و خالص پرداختی
/// </summary>
public class PayrollCalculatorTests
{
    /// <summary>بیمه سهم کارمند دقیقاً ۷٪ مزایای مشمول است</summary>
    [Fact]
    public void Insurance_IsSevenPercent_OfGrossEarnings()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 300_000_000m, OvertimeHours: 0, Bonus: 0));

        // ۳۰۰م × ۷٪ = ۲۱م ریال
        result.Insurance.Should().Be(21_000_000m);
    }

    /// <summary>اضافه‌کاری ۱/۴ برابر نرخ ساعت عادی (پایه ÷ ۱۹۲)</summary>
    [Fact]
    public void Overtime_IsOnePointFourTimes_HourlyRate()
    {
        // پایه ۱۹۲م ریال → هر ساعت ۱م → اضافه‌کاری ۴۰ ساعت × ۱/۴ = ۵۶م
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 192_000_000m, OvertimeHours: 40m));

        result.OvertimePay.Should().Be(56_000_000m);
    }

    /// <summary>حقوق زیر سقف معافیت ماهانه بدون مالیات است</summary>
    [Fact]
    public void Tax_BelowMonthlyExemption_IsZero()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 200_000_000m));

        result.Tax.Should().Be(0);
    }

    /// <summary>پله اول — ۱۰٪ فقط بر مازاد معافیت (نه کل مبلغ)</summary>
    [Fact]
    public void Tax_FirstBracket_TenPercentOnExcessOnly()
    {
        // مبنای مشمول: ۳۰۰م − بیمه ۲۱م = ۲۷۹م → مازاد بر ۲۴۰م = ۳۹م → مالیات ۳/۹م
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 300_000_000m));

        result.Tax.Should().Be(3_900_000m);
    }

    /// <summary>پلکان تجمعی چندپله‌ای — مازاد هر پله با نرخ خودش</summary>
    [Fact]
    public void Tax_SecondBracket_IsProgressive()
    {
        // مبنای مشمول ۵۰۰م: (۳۶۰−۲۴۰)×۱۰٪ + (۵۰۰−۳۶۰)×۱۵٪ = ۱۲م + ۲۱م = ۳۳م
        var tax = PayrollCalculator.ProgressiveTax(500_000_000m);

        tax.Should().Be(33_000_000m);
    }

    /// <summary>پله سوم ۲۰٪ بر مازاد ۶۰۰م</summary>
    [Fact]
    public void Tax_ThirdBracket_TwentyPercent()
    {
        // (۱۲۰×۱۰٪) + (۲۴۰×۱۵٪) + (۱۰۰×۲۰٪) = ۱۲ + ۳۶ + ۲۰ = ۶۸م
        var tax = PayrollCalculator.ProgressiveTax(700_000_000m);

        tax.Should().Be(68_000_000m);
    }

    /// <summary>خالص = کل مزایا − بیمه − مالیات − سایر کسورات</summary>
    [Fact]
    public void NetAmount_IsGrossMinusAllDeductions()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 250_000_000m, OvertimeHours: 10m, Bonus: 5_000_000m,
            OtherDeductions: 3_000_000m));

        var expectedNet = result.GrossEarnings - result.Insurance - result.Tax - 3_000_000m;
        result.NetAmount.Should().Be(expectedNet);
        result.TotalDeductions.Should().Be(result.Insurance + result.Tax + 3_000_000m);
    }

    /// <summary>حقوق بدون اضافه‌کاری و پاداش — جمع صفر نباید خطا بدهد</summary>
    [Fact]
    public void Calculate_WithZeroValues_Completes()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 0m, OvertimeHours: 0m, Bonus: 0m, OtherDeductions: 0m));

        result.OvertimePay.Should().Be(0);
        result.Insurance.Should().Be(0);
        result.Tax.Should().Be(0);
        result.NetAmount.Should().Be(0);
    }

    /// <summary>معافیت دقیقاً مرز — یک ریال بالاتر مشمول پله اول است</summary>
    [Fact]
    public void Tax_ExactlyAtExemptionBoundary()
    {
        PayrollCalculator.ProgressiveTax(240_000_000m).Should().Be(0);
        // یک ریال مازاد → ۱۰٪ یک ریال → گرد به صفر؟ نه — ۰/۱ ریال گرد به ۰
        PayrollCalculator.ProgressiveTax(240_000_100m).Should().Be(10m);
    }
}
