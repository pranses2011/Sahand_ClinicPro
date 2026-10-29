using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class PhysioPrintServiceTests
{
    [Fact]
    public void Print_snapshot_requires_patient_and_nonempty_text()
    {
        var patient = new QuickPatient(7, " بیمار ", "۱۲۳", "", null, null, null);
        PhysioPrintService.TryCreate(null, "تمرین", out _).Should().BeFalse();
        PhysioPrintService.TryCreate(patient, "  ", out _).Should().BeFalse();
        PhysioPrintService.TryCreate(patient with { Id = 0 }, "تمرین", out _).Should().BeFalse();
        PhysioPrintService.TryCreate(patient, " برنامه ", out var data).Should().BeTrue();
        data!.PatientId.Should().Be(7);
        data.PatientName.Should().Be("بیمار");
        data.Text.Should().Be("برنامه");
    }
}
