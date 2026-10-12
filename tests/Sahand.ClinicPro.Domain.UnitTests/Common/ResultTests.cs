using FluentAssertions;
using Sahand.ClinicPro.Domain.Common;
using Xunit;

namespace Sahand.ClinicPro.Domain.UnitTests.Common;

/// <summary>
/// تست‌های الگوی Result و مدل خطای دامنه — چرخهٔ ۵۱:
/// این الگو قرارداد برگردان همهٔ هندلرهای Application است (موفقیت بدون
/// استثنا / شکست با خطای فارسی) — هر رفتار مبهم این‌جا مستقیماً به
/// چند صد هندلر سرایت می‌کند.
/// </summary>
public class ResultTests
{
    // ═══════════════ Result ساده ═══════════════

    [Fact]
    public void Success_Should_Be_Successful_With_No_Error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_Should_Carry_The_Error()
    {
        var error = new Error("PATIENT_DUPLICATE", "بیمار با این کد ملی قبلاً ثبت شده است");
        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        result.Error.Message.Should().Contain("کد ملی");
    }

    // ═══════════════ Result ژنریک ═══════════════

    [Fact]
    public void Generic_Success_Should_Expose_Value()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Generic_Failure_Value_Access_Should_Throw_Persian_Guard()
    {
        var result = Result<int>.Failure(new Error("X", "خطای آزمایشی"));

        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ناموفق مجاز نیست*",
                because: "خواندن مقدار از نتیجهٔ شکست‌خورده باگ پنهان است — گارد فارسی باید بلندش کند");
    }

    // ═══════════════ مدل خطا ═══════════════

    [Fact]
    public void Error_None_Should_Be_Empty()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_NotFound_Should_Build_Code_And_Persian_Message()
    {
        var error = Error.NotFound("patient", 5);

        error.Code.Should().Be("PATIENT_NOT_FOUND");
        error.Message.Should().Contain("patient").And.Contain("5");
    }

    [Fact]
    public void Errors_With_Same_Parts_Should_Be_Equal()
    {
        // رکورد — برابری مقداری؛ کلید دیکشنری/مقایسه در لایه‌ها به آن تکیه می‌کند
        new Error("A", "پیام").Should().Be(new Error("A", "پیام"));
        new Error("A", "پیام").Should().NotBe(new Error("B", "پیام"));
    }
}
