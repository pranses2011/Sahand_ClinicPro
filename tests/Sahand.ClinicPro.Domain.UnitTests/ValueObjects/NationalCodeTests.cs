using FluentAssertions;
using Sahand.ClinicPro.Domain.Exceptions;
using Sahand.ClinicPro.Domain.ValueObjects;
using Xunit;

namespace Sahand.ClinicPro.Domain.UnitTests.ValueObjects;

/// <summary>
/// تست‌های کامل کد ملی ۱۰ رقمی — چرخهٔ ۵۱:
/// الگوریتم رقم کنترلی وزارت کشور با دو شاخه (باقیمانده کمتر از ۲ و بزرگ‌تر/
/// مساوی ۲)، رد ارقام تکراری، نرمال‌سازی فاصله‌ها و پیام فارسی خطای دامنه.
/// اعدادِ نمونه با محاسبهٔ دستی رقم کنترلی ساخته و راستی‌آزمایی شده‌اند.
/// </summary>
public class NationalCodeTests
{
    // ═══════════════ کدهای معتبر (هر دو شاخهٔ الگوریتم) ═══════════════

    [Theory]
    [InlineData("1234567891")] // جمع وزنی ۲۱۰ ÷ ۱۱ باقی‌مانده ۱ (<۲ ⟵ رقم کنترلی = خودِ باقی‌مانده)
    [InlineData("0023456787")] // باقی‌مانده ۴ (≥۲ ⟵ رقم کنترلی = ۱۱−۴)
    [InlineData("1583577963")] // باقی‌مانده ۸ (≥۲ ⟵ ۱۱−۸=۳)
    [InlineData("0499371895")] // باقی‌مانده ۶ (≥۲ ⟵ ۱۱−۶=۵)
    public void IsValid_With_Correct_Check_Digit_Should_Be_True(string code)
    {
        NationalCode.IsValid(code).Should().BeTrue(
            because: "رقم کنترلی {0} با الگوریتم وزنی سازگار است", code);
    }

    [Theory]
    [InlineData("1234567890")] // رقم درست ۱ است نه ۰
    [InlineData("0023456789")] // رقم درست ۷ است نه ۹
    [InlineData("1583577960")] // رقم درست ۳ است نه ۰
    public void IsValid_With_Wrong_Check_Digit_Should_Be_False(string code)
    {
        NationalCode.IsValid(code).Should().BeFalse(
            because: "رقم کنترلی با الگوریتم وزنی نمی‌خواند");
    }

    // ═══════════════ قالب‌های نامعتبر ═══════════════

    [Theory]
    [InlineData("12345678")]      // ۸ رقم — کوتاه
    [InlineData("123456789")]     // ۹ رقم — کوتاه
    [InlineData("12345678910")]   // ۱۱ رقم — بلند
    [InlineData("12345678a1")]    // نویسه غیرعددی
    [InlineData("12345 67891")]   // فاصلهٔ میانی پس از نرمال‌سازی هنوز نامعتبر است
    [InlineData("۱۲۳۴۵۶۷۸۹۱")]     // ارقام فارسی — تنها ارقام ASCII پذیرفته می‌شود
    public void IsValid_With_Malformed_Input_Should_Be_False(string code)
    {
        NationalCode.IsValid(code).Should().BeFalse();
    }

    [Theory]
    [InlineData("1111111111")] // همه ۱ — رقم کنترلی الگوریتمیِ «درست» دارد اما رد می‌شود
    [InlineData("0000000000")] // همه ۰
    [InlineData("9999999999")] // همه ۹
    public void IsValid_With_All_Same_Digits_Should_Be_False(string code)
    {
        NationalCode.IsValid(code).Should().BeFalse(
            because: "کد ملی با همهٔ ارقام یکسان همیشه جعلی است — الگوریتم رقم کنترلی نباید فریبش بخورد");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_With_Null_Or_Whitespace_Should_Be_False(string? code)
    {
        NationalCode.IsValid(code).Should().BeFalse();
    }

    // ═══════════════ ساخت و نرمال‌سازی ═══════════════

    [Fact]
    public void Of_With_Surrounding_Spaces_Should_Trim_And_Accept()
    {
        var nc = NationalCode.Of("  1234567891  ");
        nc.Value.Should().Be("1234567891",
            because: "نرمال‌سازی فاصله‌های ابتدا/انتها پیش از اعتبارسنجی انجام می‌شود");
    }

    [Fact]
    public void Of_With_Invalid_Code_Should_Throw_Persian_Business_Rule()
    {
        var act = () => NationalCode.Of("1234567890");
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("کد ملی نامعتبر است");
    }

    // ═══════════════ برابری مقداری ═══════════════

    [Fact]
    public void Equality_Should_Be_By_Value()
    {
        NationalCode.Of("1234567891").Should().Be(NationalCode.Of("1234567891"));
        (NationalCode.Of("1234567891") == NationalCode.Of("1234567891")).Should().BeTrue();
        NationalCode.Of("1234567891").Should().NotBe(NationalCode.Of("0499371895"));
    }
}
