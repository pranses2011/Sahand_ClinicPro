using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class GlassesPrescriptionPrintTests
{
    [Fact]
    public void Persian_numbers_are_validated_and_zero_is_not_missing()
    {
        var ok = GlassesPrescriptionPrint.TryCreate(7, "بیمار", "۰۰۱۲", "−۲٫۵", "۰", "۹۰", "+۱٫۵", null, null, null, null, "۶۲٫۵", out var data);
        ok.Should().BeTrue();
        data!.PatientId.Should().Be(7);
        data.RightSphere.Should().Be(-2.5m);
        data.RightCylinder.Should().Be(0m);
        data.RightAxis.Should().Be(90);
        data.LeftSphere.Should().BeNull();
        data.Pd.Should().Be(62.5m);
    }

    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("۱٫۲۳۴", "", "", "")]
    [InlineData("۱", "۱۸۱", "", "")]
    [InlineData("۱", "۹۰", "۶۲٫۵۵", "")]
    [InlineData("۱", "۹۰", "۶۲", "نامعتبر")]
    public void Invalid_or_empty_prescriptions_cannot_be_printed(string sphere, string axis, string pd, string add)
    {
        GlassesPrescriptionPrint.TryCreate(1, "بیمار", "", sphere, null, axis, add,
            null, null, null, null, pd, out _).Should().BeFalse();
    }

    [Fact]
    public void Missing_patient_identity_cannot_be_printed()
    {
        GlassesPrescriptionPrint.TryCreate(0, "بیمار", "", "۱", null, null, null,
            null, null, null, null, null, out _).Should().BeFalse();
    }
}
