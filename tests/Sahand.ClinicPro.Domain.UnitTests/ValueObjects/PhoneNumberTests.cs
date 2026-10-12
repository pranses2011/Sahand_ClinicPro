using FluentAssertions;
using Sahand.ClinicPro.Domain.Exceptions;
using Sahand.ClinicPro.Domain.ValueObjects;
using Xunit;

namespace Sahand.ClinicPro.Domain.UnitTests.ValueObjects;

/// <summary>
/// تست‌های شماره موبایل ایرانی — چرخهٔ ۵۱:
/// الگوی ۰۹XXXXXXXXX (۱۱ رقم)، نرمال‌سازی فاصله، رد پیش‌وندهای غلط
/// (+98 / بدون صفر / شماره ثابت) و پیام فارسی خطای دامنه.
/// </summary>
public class PhoneNumberTests
{
    [Theory]
    [InlineData("09123456789")]
    [InlineData("09351234567")]
    [InlineData("09012345678")]
    public void IsValid_With_Iranian_Mobile_Pattern_Should_Be_True(string number)
    {
        PhoneNumber.IsValid(number).Should().BeTrue();
    }

    [Theory]
    [InlineData("9123456789")]      // بدون صفر ابتدا — ۱۰ رقم
    [InlineData("0912345678")]      // یک رقم کم
    [InlineData("091234567890")]    // یک رقم زیاد
    [InlineData("02112345678")]     // پیش‌شماره ثابت تهران نه موبایل
    [InlineData("0912345678a")]     // نویسه غیرعددی
    [InlineData("+989123456789")]   // فرمت بین‌المللی پذیرفته نمی‌شود
    [InlineData("09-123456789")]    // جداکننده
    [InlineData("۰۹۱۲۳۴۵۶۷۸۹")]      // ارقام فارسی — تنها ASCII
    public void IsValid_With_Malformed_Input_Should_Be_False(string number)
    {
        PhoneNumber.IsValid(number).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_With_Null_Or_Whitespace_Should_Be_False(string? number)
    {
        PhoneNumber.IsValid(number).Should().BeFalse();
    }

    [Fact]
    public void Of_With_Surrounding_Spaces_Should_Trim_And_Accept()
    {
        var phone = PhoneNumber.Of("  09123456789  ");
        phone.Value.Should().Be("09123456789");
    }

    [Fact]
    public void Of_With_Invalid_Number_Should_Throw_With_Persian_Pattern_Hint()
    {
        var act = () => PhoneNumber.Of("02112345678");
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*09XXXXXXXXX*",
                because: "پیام خطا باید الگوی درست را به کاربر نشان دهد");
    }

    [Fact]
    public void Equality_Should_Be_By_Value()
    {
        PhoneNumber.Of("09123456789").Should().Be(PhoneNumber.Of("09123456789"));
        PhoneNumber.Of("09123456789").Should().NotBe(PhoneNumber.Of("09351234567"));
    }
}
