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
            BaseSalary: 300_000_000m, OvertimeHours: 0, Bonus: 0, IsInsured: true));

        // ۳۰۰م × ۷٪ = ۲۱م ریال
        result.Insurance.Should().Be(21_000_000m);
    }

    /// <summary>بدون انتخاب بیمه، سهم بیمه از فیش کسر نمی‌شود</summary>
    [Fact]
    public void Insurance_IsZero_WhenEmployeeIsNotInsured()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 300_000_000m, IsInsured: false));

        result.Insurance.Should().Be(0m);
        result.TaxableBase.Should().Be(result.GrossEarnings);
        result.TotalDeductions.Should().Be(result.Tax);
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
            BaseSalary: 300_000_000m, IsInsured: true));

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

    // ═══════════ حقوق درصدی از سود خدمات — چرخه نهم ═══════════

    /// <summary>حقوق درصدی = سود خدمات انجامی خود پرسنل × درصد (نه کل فروش)</summary>
    [Fact]
    public void Commission_IsPercentOfServiceProfit_NotTotalRevenue()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 200_000_000m,
            CommissionPercent: 30m,
            ServiceProfit: 100_000_000m));

        // ۳۰٪ از سود ۱۰۰م = ۳۰م — نه ۳۰٪ از فروش یا کل دریافتی
        result.ServiceCommission.Should().Be(30_000_000m);
    }

    /// <summary>سود منفی دوره → حقوق درصدی صفر (ضرر بر عهده کارفرماست)</summary>
    [Fact]
    public void Commission_NegativeProfit_IsZero()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 200_000_000m,
            CommissionPercent: 30m,
            ServiceProfit: -50_000_000m));

        result.ServiceCommission.Should().Be(0m);
    }

    /// <summary>درصد صفر (فقط حقوق پایه) → بدون حقوق درصدی</summary>
    [Fact]
    public void Commission_ZeroPercent_IsZero()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 200_000_000m,
            CommissionPercent: 0m,
            ServiceProfit: 100_000_000m));

        result.ServiceCommission.Should().Be(0m);
    }

    /// <summary>حقوق درصدی جزو مزایای مشمول بیمه و مالیات است (ماده ۲۸ ت.ا)</summary>
    [Fact]
    public void Commission_IsIncludedInGross_AndSubjectToInsurance()
    {
        var without = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 200_000_000m, IsInsured: true));
        var with = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 200_000_000m,
            CommissionPercent: 25m,
            ServiceProfit: 80_000_000m,
            IsInsured: true)); // کمیسیون ۲۰م

        // جمع مزایا دقیقاً به اندازه کمیسیون رشد می‌کند
        with.GrossEarnings.Should().Be(without.GrossEarnings + 20_000_000m);
        // بیمه ۷٪ از کل مزایای جدید (شامل کمیسیون): اختلاف = ۷٪ کمیسیون
        with.Insurance.Should().Be(Math.Round((200_000_000m + 20_000_000m) * 0.07m));
    }

    /// <summary>گرد شدن حقوق درصدی به ریال کامل</summary>
    [Fact]
    public void Commission_RoundsToWholeRial()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 100_000_000m,
            CommissionPercent: 33m,
            ServiceProfit: 10_000_003m));

        // ۳۳٪ از ۱۰٬۰۰۰٬۰۰۳ = ۳٬۳۰۰٬۰۰۰/۹۹ → ۳٬۳۰۰٬۰۰۱ ریال
        result.ServiceCommission.Should().Be(3_300_001m);
    }

    /// <summary>خالص پرداختی با کمیسیون — کل زنجیره مزایا/کسورات متوازن است</summary>
    [Fact]
    public void NetAmount_WithCommission_Balances()
    {
        var result = PayrollCalculator.Calculate(new PayrollCalculator.PayrollInput(
            BaseSalary: 150_000_000m,
            CommissionPercent: 40m,
            ServiceProfit: 50_000_000m)); // کمیسیون ۲۰م

        var expectedGross = 150_000_000m + 20_000_000m;
        result.GrossEarnings.Should().Be(expectedGross);
        // بدون کسور اضافی: خالص = جمع مزایا − بیمه − مالیات
        result.NetAmount.Should().Be(expectedGross - result.Insurance - result.Tax);
    }
}
