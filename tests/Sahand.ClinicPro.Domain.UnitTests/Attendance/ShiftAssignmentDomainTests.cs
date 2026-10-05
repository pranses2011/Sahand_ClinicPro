using Xunit;
using FluentAssertions;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.UnitTests.Attendance;

/// <summary>
/// تست‌های واحد منطق دامنه ماژول حضور و غیاب — طول شیفت و تخصیص مؤثر
/// </summary>
public class ShiftDomainTests
{
    /// <summary>شیفت روزانه ۸ تا ۱۶ — ۴۸۰ دقیقه</summary>
    [Fact]
    public void DurationMinutes_DayShift_ShouldBe480()
    {
        var shift = new Shift { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0) };

        shift.DurationMinutes.Should().Be(480);
    }

    /// <summary>شیفت شب ۲۰ تا ۴ — ۴۸۰ دقیقه با عبور از نیمه‌شب</summary>
    [Fact]
    public void DurationMinutes_NightShift_ShouldWrapMidnight()
    {
        var shift = new Shift { StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(4, 0), IsNightShift = true };

        shift.DurationMinutes.Should().Be(480);
    }

    /// <summary>شیفت ۲۴ ساعته — ۱۴۴۰ دقیقه</summary>
    [Fact]
    public void DurationMinutes_FullDay_ShouldBe1440()
    {
        var shift = new Shift { StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(0, 0) };

        shift.DurationMinutes.Should().Be(1440);
    }
}

/// <summary>
/// تست‌های تخصیص شیفت — الگوی تکرار و روزهای هفته ایرانی
/// </summary>
public class ShiftAssignmentDomainTests
{
    /// <summary>شنبه ۲۰ سپتامبر ۲۰۲۵ — بیت ۰</summary>
    private static readonly DateOnly Saturday = new(2025, 9, 20);

    /// <summary>یکشنبه — بیت ۱</summary>
    private static readonly DateOnly Sunday = new(2025, 9, 21);

    /// <summary>جمعه — بیت ۶</summary>
    private static readonly DateOnly Friday = new(2025, 9, 26);

    /// <summary>روز هفته ایرانی: شنبه=۰ تا جمعه=۶</summary>
    [Fact]
    public void PersianDayOfWeek_ShouldMapSaturdayToZero()
    {
        EmployeeShiftAssignment.PersianDayOfWeek(Saturday).Should().Be(0);
        EmployeeShiftAssignment.PersianDayOfWeek(Sunday).Should().Be(1);
        EmployeeShiftAssignment.PersianDayOfWeek(Friday).Should().Be(6);
    }

    /// <summary>تخصیص ثابت — همه روزهای بازه فعال است</summary>
    [Fact]
    public void IsEffectiveOn_FixedAssignment_ShouldCoverEveryDay()
    {
        var assignment = new EmployeeShiftAssignment
        {
            FromDate = new DateOnly(2025, 9, 1),
            ToDate = null,
            Recurrence = ShiftAssignmentRecurrence.Fixed,
            IsActive = true,
        };

        assignment.IsEffectiveOn(Friday).Should().BeTrue();
        assignment.IsEffectiveOn(Saturday).Should().BeTrue();
    }

    /// <summary>تخصیص هفتگی شنبه تا چهارشنبه — پنجشنبه و جمعه تعطیل</summary>
    [Fact]
    public void IsEffectiveOn_WeekdaysOnly_ShouldExcludeThursdayFriday()
    {
        // شنبه تا چهارشنبه = بیت‌های ۰..۴ → 0b0011111 = 31
        var assignment = new EmployeeShiftAssignment
        {
            FromDate = new DateOnly(2025, 9, 1),
            Recurrence = ShiftAssignmentRecurrence.Weekly,
            WeekDaysMask = 0b0011111,
            IsActive = true,
        };

        assignment.IsEffectiveOn(Saturday).Should().BeTrue();   // شنبه
        assignment.IsEffectiveOn(Friday).Should().BeFalse();    // جمعه
    }

    /// <summary>پایان بازه — پس از ToDate غیرفعال است</summary>
    [Fact]
    public void IsEffectiveOn_AfterEndDate_ShouldBeInactive()
    {
        var assignment = new EmployeeShiftAssignment
        {
            FromDate = new DateOnly(2025, 9, 1),
            ToDate = new DateOnly(2025, 9, 15),
            Recurrence = ShiftAssignmentRecurrence.Fixed,
            IsActive = true,
        };

        assignment.IsEffectiveOn(new DateOnly(2025, 9, 16)).Should().BeFalse();
        assignment.IsEffectiveOn(new DateOnly(2025, 9, 10)).Should().BeTrue();
    }

    /// <summary>تخصیص غیرفعال — هیچ روزی مؤثر نیست</summary>
    [Fact]
    public void IsEffectiveOn_InactiveAssignment_ShouldNeverApply()
    {
        var assignment = new EmployeeShiftAssignment
        {
            FromDate = new DateOnly(2025, 9, 1),
            Recurrence = ShiftAssignmentRecurrence.Fixed,
            IsActive = false,
        };

        assignment.IsEffectiveOn(Saturday).Should().BeFalse();
    }
}
