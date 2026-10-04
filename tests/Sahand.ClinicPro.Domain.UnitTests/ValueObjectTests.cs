using FluentAssertions;
using Sahand.ClinicPro.Domain.ValueObjects;
using Xunit;

namespace Sahand.ClinicPro.Domain.UnitTests;

/// <summary>
/// تست‌های اشیاء ارزشی دامنه — پول و کد ملی و موبایل
/// </summary>
public class ValueObjectTests
{
    // ═══════════ Money ═══════════

    [Fact]
    public void Money_Of_Should_Round_To_Zero_Decimals()
    {
        // عملیات: ساخت مبلغ با اعشار
        var money = Money.Of(1234.56m);

        // نتیجه: گرد شده به ریال صحیح
        money.Amount.Should().Be(1235m, because: "مبالغ ریالی بدون اعشار ذخیره می‌شوند");
    }

    [Fact]
    public void Money_Add_Should_Sum_Amounts()
    {
        var a = Money.Of(100m);
        var b = Money.Of(200m);

        var sum = a.Add(b);

        sum.Amount.Should().Be(300m);
        sum.Should().Be(Money.Of(300m), because: "برابری اشیاء ارزشی بر اساس مقدار است");
    }

    [Fact]
    public void Money_Subtract_Should_Allow_Negative()
    {
        var result = Money.Of(100m).Subtract(Money.Of(250m));

        result.IsNegative.Should().BeTrue(because: "تفریق ممکن است منفی شود — مثل بدهی");
        result.Amount.Should().Be(-150m);
    }

    [Fact]
    public void Money_Percentage_Should_Compute()
    {
        var result = Money.Of(200_000m).Percentage(10m);

        result.Amount.Should().Be(20_000m, because: "ده درصد دویست هزار = بیست هزار");
    }

    // ═══════════ NationalCode ═══════════

    [Theory]
    [InlineData("0012345679")]   // نمونه معتبر
    [InlineData("0076409627")]
    public void NationalCode_Valid_Codes_Should_Pass(string code)
    {
        NationalCode.IsValid(code).Should().BeTrue(because: $"کد {code} رقم کنترلی معتبر دارد");
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("0000000000")]   // همه صفر — رد
    [InlineData("1111111111")]   // ارقام تکراری — رد
    [InlineData("12345")]        // کوتاه
    [InlineData("abcdefghij")]   // غیرعددی
    [InlineData("")]
    public void NationalCode_Invalid_Codes_Should_Fail(string code)
    {
        NationalCode.IsValid(code).Should().BeFalse(because: $"کد {code} باید رد شود");
    }

    [Fact]
    public void NationalCode_Of_Should_Throw_For_Invalid()
    {
        var act = () => NationalCode.Of("1234567890");

        act.Should().Throw<Domain.Exceptions.BusinessRuleException>()
            .WithMessage("کد ملی نامعتبر است");
    }

    // ═══════════ PhoneNumber ═══════════

    [Theory]
    [InlineData("09121234567", true)]
    [InlineData("09351234567", true)]
    [InlineData("9121234567", false)]
    [InlineData("0912123456", false)]
    [InlineData("091212345678", false)]
    public void PhoneNumber_Pattern_Should_Validate(string number, bool expected)
    {
        PhoneNumber.IsValid(number).Should().Be(expected);
    }
}
