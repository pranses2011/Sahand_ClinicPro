using FluentAssertions;
using Sahand.ClinicPro.Domain.ValueObjects;
using Xunit;

namespace Sahand.ClinicPro.Domain.UnitTests.ValueObjects;

/// <summary>
/// تست‌های کامل شیء ارزشی Money — چرخهٔ ۵۱ (ضلع گمشدهٔ تست دامنه):
/// پول پرریسک‌ترین شیء ارزشی سامانه است (فاکتور، پرداخت، کارانه، حسابداری)
/// و تا این چرخه فقط ۴ تست پایه داشت. این‌جا گرد کردن ریال، عملیات چهارگانه،
/// علامت، برابری مقداری و قالب متنی پوشش داده می‌شود.
/// </summary>
public class MoneyTests
{
    // ═══════════════ گرد کردن ریال ═══════════════

    [Theory]
    [InlineData(1234.56, 1235)]   // اعشار بالای نیمه — به بالا
    [InlineData(1234.4, 1234)]    // اعشار زیر نیمه — به پایین
    [InlineData(0.6, 1)]
    [InlineData(0.4, 0)]
    [InlineData(987654321.99, 987654322)]
    [InlineData(0, 0)]
    [InlineData(-1500.75, -1501)] // منفی هم گرد می‌شود
    public void Of_Should_Round_To_Integer_Rial(decimal input, decimal expected)
    {
        Money.Of(input).Amount.Should().Be(expected,
            because: "مبالغ ریالی در دامنه همیشه عدد صحیح‌اند");
    }

    [Fact]
    public void Zero_Should_Be_Amount_Zero()
    {
        Money.Zero.Amount.Should().Be(0m);
        Money.Zero.IsNegative.Should().BeFalse();
    }

    // ═══════════════ عملیات چهارگانه ═══════════════

    [Fact]
    public void Add_Should_Sum_Two_Amounts()
    {
        var sum = Money.Of(1_500_000m).Add(Money.Of(500_000m));
        sum.Amount.Should().Be(2_000_000m);
    }

    [Fact]
    public void Subtract_Should_Produce_Negative_When_Other_Is_Larger()
    {
        var result = Money.Of(100_000m).Subtract(Money.Of(450_000m));
        result.Amount.Should().Be(-350_000m);
        result.IsNegative.Should().BeTrue(because: "مانده بدهی بیمار منفی است");
    }

    [Fact]
    public void Subtract_To_Zero_Should_Not_Be_Negative()
    {
        Money.Of(200m).Subtract(Money.Of(200m)).IsNegative.Should().BeFalse();
    }

    [Theory]
    [InlineData(250_000, 3, 750_000)]
    [InlineData(10_000, 0, 0)]          // ضریب صفر = صفر
    [InlineData(10_000, 1.5, 15_000)]   // ضریب اعشاری بدون باقی‌مانده
    public void Multiply_Should_Apply_Factor(decimal amount, decimal factor, decimal expected)
    {
        Money.Of(amount).Multiply(factor).Amount.Should().Be(expected);
    }

    [Theory]
    [InlineData(200_000, 10, 20_000)]
    [InlineData(999, 1, 10)]            // ۹٫۹۹ ریال → گرد به ۱۰
    [InlineData(500_000, 0, 0)]         // صفر درصد = صفر
    [InlineData(500_000, 100, 500_000)] // صد درصد = خودش
    public void Percentage_Should_Compute_And_Round(decimal amount, decimal percent, decimal expected)
    {
        Money.Of(amount).Percentage(percent).Amount.Should().Be(expected);
    }

    // ═══════════════ برابری مقداری (شیء ارزشی) ═══════════════

    [Fact]
    public void Equality_Should_Be_By_Amount_Not_Reference()
    {
        var a = Money.Of(125_000m);
        var b = Money.Of(125_000m);

        a.Should().NotBeSameAs(b);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Different_Amounts_Should_Not_Be_Equal()
    {
        // برابری مقداری: مبلغ متفاوت = نابرابر (حتی با نوع یکسان)
        (Money.Of(1m) == Money.Of(2m)).Should().BeFalse();
        Money.Of(1m).Equals(Money.Of(2m)).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_Match_For_Equal_Amounts()
    {
        Money.Of(777m).GetHashCode().Should().Be(Money.Of(777m).GetHashCode());
    }

    // ═══════════════ قالب متنی و علامت ═══════════════

    [Fact]
    public void ToString_Should_Contain_Currency_Suffix()
    {
        Money.Of(1000m).ToString().Should().Contain("IRR",
            because: "واحد پیش‌فرض ریال است و در قالب متنی می‌آید");
    }

    [Fact]
    public void IsNegative_Should_Be_False_For_Positive_And_Zero()
    {
        Money.Of(1m).IsNegative.Should().BeFalse();
        Money.Of(0m).IsNegative.Should().BeFalse();
    }
}
