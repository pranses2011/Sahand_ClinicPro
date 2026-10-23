using FluentAssertions;
using Sahand.ClinicPro.Application.Common.Helpers;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Doctors;

public class DoctorScheduleTimeTests
{
    [Fact]
    public void NightShift_EndBeforeStart_IsValidAndCrossesMidnight()
    {
        var start = new TimeOnly(20, 0);
        var end = new TimeOnly(4, 0);

        DoctorScheduleTime.IsValidRange(start, end).Should().BeTrue();
        DoctorScheduleTime.CrossesMidnight(start, end).Should().BeTrue();
        DoctorScheduleTime.DurationMinutes(start, end).Should().Be(480);
    }

    [Fact]
    public void DayShift_DurationIsCalculatedNormally()
    {
        DoctorScheduleTime.CrossesMidnight(new TimeOnly(8, 0), new TimeOnly(16, 0)).Should().BeFalse();
        DoctorScheduleTime.DurationMinutes(new TimeOnly(8, 0), new TimeOnly(16, 0)).Should().Be(480);
    }

    [Fact]
    public void EqualStartAndEnd_IsInvalidRatherThanAnAccidentalFullDayShift()
    {
        var time = new TimeOnly(8, 0);

        DoctorScheduleTime.IsValidRange(time, time).Should().BeFalse();
        DoctorScheduleTime.DurationMinutes(time, time).Should().Be(0);
    }
}
