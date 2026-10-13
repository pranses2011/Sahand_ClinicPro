using FluentAssertions;
using Sahand.ClinicPro.Application.Common.Helpers;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

/// <summary>
/// تست‌های واحد گارد صفحه‌بندی — چرخهٔ ۶۲ (P-03).
/// گزاره‌ها: صفحهٔ صفر/منفی به یک می‌رسد، اندازهٔ غول‌پیکر سقف‌خورده،
/// و Skip همیشه نامنفی است (استثنای ترجمهٔ EF در درخواست‌های معیوب).
/// </summary>
public class PagingGuardTests
{
    /// <summary>صفحهٔ کمتر از ۱ → ۱ (ریشهٔ Skip منفی)</summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(17, 17)]
    public void Page_Clamps_Below_One(int input, int expected)
        => PagingGuard.Page(input).Should().Be(expected, because: "شمارهٔ صفحهٔ معتبر از ۱ شروع می‌شود");

    /// <summary>اندازهٔ نامعتبر — صفر/منفی به ۲۰ و بزرگ‌تر از سقف به سقف می‌رسد</summary>
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-3, 20)]
    [InlineData(50, 50)]
    [InlineData(100_000, 200)]
    [InlineData(201, 200)]
    public void Size_Clamps_Into_Sane_Range(int input, int expected)
        => PagingGuard.Size(input).Should().Be(expected, because: "اندازهٔ صفحه بین ۱ و سقف است");

    /// <summary>سقف اختصاصی endpoint — audit بزرگ‌تر از ۲۰۰ نمی‌دهد</summary>
    [Fact]
    public void Size_Respects_Custom_Max()
        => PagingGuard.Size(500, max: 100).Should().Be(100, because: "سقف اختصاصی اعمال می‌شود");

    /// <summary>Skip همیشه نامنفی — پیش‌شرط ترجمهٔ EF</summary>
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 0)]
    [InlineData(3, 20, 40)]
    public void Skip_Is_Always_Non_Negative(int page, int size, int expected)
        => PagingGuard.Skip(page, size).Should().Be(expected, because: "Skip منفی استثنای ترجمه می‌سازد");

    /// <summary>زوج Clamp — ترکیب هر دو گارد در یک فراخوانی</summary>
    [Fact]
    public void Clamp_Returns_Sanitized_Pair()
    {
        var (page, size) = PagingGuard.Clamp(-2, 100_000, 200);
        page.Should().Be(1);
        size.Should().Be(200);
    }
}
