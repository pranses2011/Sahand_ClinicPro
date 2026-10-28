using FluentAssertions;
using Sahand.ClinicPro.Application.Features.HR;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.HR;

/// <summary>
/// نگهبان «سمت دستیتایپشده ذخیره نمیشود» — چرخهٔ جاری.
/// قانون طلایی: نام ارسالشده از فرم همیشه بر شناسهٔ کهنه مقدم است.
/// </summary>
public class EmployeeLookupResolverTests
{
    [Fact]
    public void Decide_Name_Always_Wins_Over_Stale_Id()
    {
        var (kind, name) = EmployeeLookupResolver.Decide(7L, "پزشک عمومی");
        kind.Should().Be(EmployeeLookupResolver.Resolution.FindOrCreate);
        name.Should().Be("پزشک عمومی");
    }

    [Fact]
    public void Decide_Legacy_Client_Without_Name_Keeps_Id()
    {
        var (kind, _) = EmployeeLookupResolver.Decide(7L, null);
        kind.Should().Be(EmployeeLookupResolver.Resolution.UseId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("—")]
    public void Decide_Empty_Or_Marker_Clears_Value(string value)
    {
        var (kind, _) = EmployeeLookupResolver.Decide(7L, value);
        kind.Should().Be(EmployeeLookupResolver.Resolution.Clear);
    }

    [Fact]
    public void Normalize_Arabic_And_Persian_Variants_Are_Equal()
    {
        EmployeeLookupResolver.NormalizeName("  منشی  ")
            .Should().Be("منشی");
        EmployeeLookupResolver.NamesEqual("منشی", "منشی").Should().BeTrue();
        EmployeeLookupResolver.NamesEqual("پزشک", "پزشک").Should().BeTrue();
        EmployeeLookupResolver.NamesEqual("  پرستار  ", "پرستار").Should().BeTrue();
    }

    [Fact]
    public void Normalize_Collapses_Internal_Whitespace_And_Zwnj()
    {
        EmployeeLookupResolver.NormalizeName("سرپرست\u200C   بخش")
            .Should().Be("سرپرست‌   بخش");
    }

    [Fact]
    public void CreateEmployeeDto_Exposes_Typed_Names()
    {
        var dto = new CreateEmployeeDto("علی", "احمدی", "1234567890", "09123456789",
            null, null, PositionName: "منشی", DepartmentName: "پذیرش");
        dto.PositionName.Should().Be("منشی");
        dto.DepartmentName.Should().Be("پذیرش");
    }
}
