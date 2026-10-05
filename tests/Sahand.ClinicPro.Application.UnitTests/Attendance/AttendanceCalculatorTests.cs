using Xunit;
using FluentAssertions;
using Sahand.ClinicPro.Application.Features.Attendance;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.UnitTests.Attendance;

/// <summary>
/// تست‌های واحد محاسباتگر حضور و غیاب — قلب ماژول T01
/// سناریوهای واقعی درمانگاه: تأخیر، تعجیل، شیفت شب، اضافه‌کاری، مرخصی و تعطیلی
/// </summary>
public class AttendanceCalculatorTests
{
    /// <summary>تاریخ پایه — سه‌شنبه ۲۳ سپتامبر ۲۰۲۵</summary>
    private static readonly DateOnly BaseDate = new(2025, 9, 23);

    /// <summary>شیفت اداری استاندارد ۸ تا ۱۶ با تحمل ۱۰ دقیقه</summary>
    private static Shift OfficeShift() => new()
    {
        ShiftName = "اداری صبح",
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(16, 0),
        LateGraceMinutes = 10,
        EarlyLeaveGraceMinutes = 10,
        MinMinutesBetweenPunches = 5,
        OvertimeStartAfterMinutes = 15,
    };

    private static DateTime At(int hour, int minute) =>
        BaseDate.ToDateTime(new TimeOnly(hour, minute));

    /// <summary>ورود و خروج سر وقت — حاضر کامل بدون تأخیر</summary>
    [Fact]
    public void Compute_OnTimeInOut_ShouldBePresentWithFullHours()
    {
        var shift = OfficeShift();

        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 0), At(16, 0)], shift);

        result.Status.Should().Be(AttendanceStatus.Present);
        result.CheckIn.Should().Be(new TimeOnly(8, 0));
        result.CheckOut.Should().Be(new TimeOnly(16, 0));
        result.LateMinutes.Should().Be(0);
        result.EarlyLeaveMinutes.Should().Be(0);
        result.WorkedHours.Should().Be(8m);
        result.OvertimeHours.Should().Be(0m);
    }

    /// <summary>تأخیر ۳۰ دقیقه با تحمل ۱۰ — ۲۰ دقیقه تأخیر و وضعیت تأخیر</summary>
    [Fact]
    public void Compute_LateBy30WithGrace10_ShouldCount20Minutes()
    {
        var shift = OfficeShift();

        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 30), At(16, 10)], shift);

        result.Status.Should().Be(AttendanceStatus.Late);
        result.LateMinutes.Should().Be(20);
        result.EarlyLeaveMinutes.Should().Be(0);
    }

    /// <summary>تأخیر داخل سقف تحمل — تأخیر شمرده نمی‌شود</summary>
    [Fact]
    public void Compute_LateWithinGrace_ShouldNotBeLate()
    {
        var shift = OfficeShift();

        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 9), At(16, 0)], shift);

        result.Status.Should().Be(AttendanceStatus.Present);
        result.LateMinutes.Should().Be(0);
    }

    /// <summary>تعجیل خروج ۳۰ دقیقه با تحمل ۱۰ — ۲۰ دقیقه تعجیل</summary>
    [Fact]
    public void Compute_EarlyLeaveBy30WithGrace10_ShouldCount20Minutes()
    {
        var shift = OfficeShift();

        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 0), At(15, 30)], shift);

        result.EarlyLeaveMinutes.Should().Be(20);
        result.WorkedHours.Should().Be(7.5m);
    }

    /// <summary>ضربه‌های همسازه تصادفی (۳ و ۴ دقیقه فاصله) حذف و فقط مؤثرها می‌مانند</summary>
    [Fact]
    public void Compute_AccidentalDoublePunches_ShouldCollapse()
    {
        var shift = OfficeShift();

        // ورود ۸:۰۰ سپس ضربه تصادفی ۸:۰۳ — و خروج ۱۶:۰۰ سپس ضربه ۱۶:۰۴
        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 0), At(8, 3), At(16, 0), At(16, 4)], shift);

        result.CheckIn.Should().Be(new TimeOnly(8, 0));
        result.CheckOut.Should().Be(new TimeOnly(16, 0));
        result.WorkedHours.Should().Be(8m);
    }

    /// <summary>شیفت شب ۲۰ تا ۴ بامداد — خروج پس از نیمه‌شب به همان روز تعلق دارد</summary>
    [Fact]
    public void Compute_NightShift_CrossingMidnight_ShouldWork8Hours()
    {
        var shift = new Shift
        {
            ShiftName = "شبانه",
            StartTime = new TimeOnly(20, 0),
            EndTime = new TimeOnly(4, 0),
            IsNightShift = true,
            LateGraceMinutes = 10,
            EarlyLeaveGraceMinutes = 10,
            MinMinutesBetweenPunches = 5,
            OvertimeStartAfterMinutes = 15,
        };

        var result = AttendanceCalculator.Compute(
            BaseDate,
            [At(20, 0), BaseDate.ToDateTime(new TimeOnly(4, 0)).AddDays(1)],
            shift);

        result.Status.Should().Be(AttendanceStatus.Present);
        result.WorkedHours.Should().Be(8m);
        result.CheckOut.Should().Be(new TimeOnly(4, 0));
    }

    /// <summary>هیچ ضربه‌ای نرسیده — غایب</summary>
    [Fact]
    public void Compute_NoPunches_ShouldBeAbsent()
    {
        var result = AttendanceCalculator.Compute(BaseDate, [], OfficeShift());

        result.Status.Should().Be(AttendanceStatus.Absent);
        result.CheckIn.Should().BeNull();
        result.WorkedHours.Should().Be(0m);
    }

    /// <summary>مرخصی استحقاقی تأییدشده بدون ضربه — وضعیت مرخصی</summary>
    [Fact]
    public void Compute_ApprovedVacation_ShouldBeLeave()
    {
        var leave = new EmployeeLeave
        {
            EmployeeId = 1,
            LeaveType = LeaveType.Vacation,
            Status = LeaveStatus.Approved,
            FromDate = BaseDate,
            ToDate = BaseDate,
        };

        var result = AttendanceCalculator.Compute(BaseDate, [], OfficeShift(), leave);

        result.Status.Should().Be(AttendanceStatus.Leave);
    }

    /// <summary>مأموریت تأییدشده — وضعیت مأموریت</summary>
    [Fact]
    public void Compute_ApprovedMission_ShouldBeMission()
    {
        var leave = new EmployeeLeave
        {
            EmployeeId = 1,
            LeaveType = LeaveType.Mission,
            Status = LeaveStatus.Approved,
            FromDate = BaseDate,
            ToDate = BaseDate,
        };

        var result = AttendanceCalculator.Compute(BaseDate, [], OfficeShift(), leave);

        result.Status.Should().Be(AttendanceStatus.Mission);
    }

    /// <summary>مرخصی ردشده غیبت را پوشش نمی‌دهد</summary>
    [Fact]
    public void Compute_RejectedLeave_ShouldNotCoverAbsence()
    {
        var leave = new EmployeeLeave
        {
            EmployeeId = 1,
            LeaveType = LeaveType.Vacation,
            Status = LeaveStatus.Rejected,
            FromDate = BaseDate,
            ToDate = BaseDate,
        };

        var result = AttendanceCalculator.Compute(BaseDate, [], OfficeShift(), leave);

        result.Status.Should().Be(AttendanceStatus.Absent);
    }

    /// <summary>تعطیل رسمی بدون ضربه — روز کار شمرده نمی‌شود (حاضر بدون کارکرد)</summary>
    [Fact]
    public void Compute_OfficialHolidayNoPunch_ShouldNotBeAbsent()
    {
        var result = AttendanceCalculator.Compute(BaseDate, [], OfficeShift(), isOfficialHoliday: true);

        result.Status.Should().Be(AttendanceStatus.Present);
        result.WorkedHours.Should().Be(0m);
    }

    /// <summary>اضافه‌کاری — خروج ۲ ساعت پس از پایان با آستانه ۱۵ دقیقه → ۱:۴۵</summary>
    [Fact]
    public void Compute_Overtime_ShouldCountAfterThreshold()
    {
        var shift = OfficeShift();

        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 0), At(18, 0)], shift);

        result.OvertimeHours.Should().Be(1.75m); // ۱۲۰ دقیقه منهای آستانه ۱۵ دقیقه = ۱۰۵ دقیقه
        result.WorkedHours.Should().Be(8m);      // کارکرد در سقف طول شیفت
    }

    /// <summary>تنها یک ضربه ورود — خروج تهی و کارکرد صفر</summary>
    [Fact]
    public void Compute_OnlyCheckIn_ShouldHaveNullCheckOut()
    {
        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 5)], OfficeShift());

        result.CheckIn.Should().Be(new TimeOnly(8, 5));
        result.CheckOut.Should().BeNull();
        result.WorkedHours.Should().Be(0m);
    }

    /// <summary>بدون شیفت تعریف‌شده — تجمیع ساده اولین تا آخرین ضربه</summary>
    [Fact]
    public void Compute_WithoutShift_ShouldAggregateSimply()
    {
        var result = AttendanceCalculator.Compute(
            BaseDate, [At(9, 15), At(13, 45)], null);

        result.Status.Should().Be(AttendanceStatus.Present);
        result.CheckIn.Should().Be(new TimeOnly(9, 15));
        result.CheckOut.Should().Be(new TimeOnly(13, 45));
        result.WorkedHours.Should().Be(4.5m);
    }

    /// <summary>کسر استراحت ۳۰ دقیقه‌ای از کارکرد</summary>
    [Fact]
    public void Compute_WithBreak_ShouldDeductBreakMinutes()
    {
        var shift = OfficeShift();
        shift.BreakMinutes = 30;

        var result = AttendanceCalculator.Compute(
            BaseDate, [At(8, 0), At(16, 30)], shift);

        result.WorkedHours.Should().Be(8m); // ۸:۳۰ منهای ۰:۳۰ استراحت = ۸ ساعت
    }

    /// <summary>ضربه پیش از پنجره (۵ ساعت قبل) نادیده گرفته می‌شود و نتیجه غیبت است</summary>
    [Fact]
    public void Compute_PunchOutsideWindow_ShouldBeIgnored()
    {
        var result = AttendanceCalculator.Compute(
            BaseDate, [At(2, 0)], OfficeShift());

        result.Status.Should().Be(AttendanceStatus.Absent);
    }
}
