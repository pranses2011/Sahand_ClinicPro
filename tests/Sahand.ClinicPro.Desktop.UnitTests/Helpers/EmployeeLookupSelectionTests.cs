using System.Collections.ObjectModel;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests.Helpers;

/// <summary>
/// نگهبان WPF: ایندکس کهنهٔ کامبوی ویرایشپذیر نباید متن تایپشده را ببلعد
/// (گزارش مالک: «فیلد سمت رو که دستی تایپ میکنم ... همون مقدار قبلی
/// روش میمونه»).
/// </summary>
public class EmployeeLookupSelectionTests
{
    private static ObservableCollection<EmployeeFormViewModel.LookupOption> Items()
        => new()
        {
            new EmployeeFormViewModel.LookupOption { Id = 1, Name = "منشی" },
            new EmployeeFormViewModel.LookupOption { Id = 2, Name = "پرستار" }
        };

    [Fact]
    public void Typed_Text_Overrides_Stale_SelectedIndex_On_Edit()
    {
        var (id, name) = EmployeeFormViewModel.ResolveLookup(
            Items(), selectedIndex: 0, text: "پزشک عمومی",
            originalId: 1, originalName: "منشی");

        id.Should().BeNull("مقدار تازه باید برای ساخت سروری فرستاده شود");
        name.Should().Be("پزشک عمومی");
    }

    [Fact]
    public void Typed_Existing_Name_Changes_The_Id()
    {
        var (id, name) = EmployeeFormViewModel.ResolveLookup(
            Items(), selectedIndex: 0, text: "پرستار",
            originalId: 1, originalName: "منشی");

        id.Should().Be(2);
        name.Should().Be("پرستار");
    }

    [Fact]
    public void SelectedIndex_Is_Used_Only_When_There_Is_No_Text()
    {
        var (id, _) = EmployeeFormViewModel.ResolveLookup(
            Items(), selectedIndex: 1, text: null,
            originalId: null, originalName: null);

        id.Should().Be(2);
    }

    [Fact]
    public void Clearing_The_Field_Is_Explicit_And_Is_Not_Overridden_By_Index()
    {
        var (id, name) = EmployeeFormViewModel.ResolveLookup(
            Items(), selectedIndex: 0, text: "  ",
            originalId: 1, originalName: "منشی");

        id.Should().BeNull();
        name.Should().BeEmpty();
    }

    [Fact]
    public void LookupName_Normalizes_Typed_Text()
    {
        LookupName.Normalize(" منشي  ").Should().Be("منشی");
        LookupName.AreEqual("پزشك", "پزشک").Should().BeTrue();
    }
}
