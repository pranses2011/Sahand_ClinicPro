using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.Features.Attendance;

/// <summary>نتیجه محاسبه یک روز حضور — خروجی محاسباتگر خالص و تست‌پذیر</summary>
/// <param name="CheckIn">اولین ضربه مؤثر ورود</param>
/// <param name="CheckOut">آخرین ضربه مؤثر خروج — تهی یعنی هنوز داخل شیفت است</param>
/// <param name="LateMinutes">دقایق تأخیر پس از کسر تحمل</param>
/// <param name="EarlyLeaveMinutes">دقایق تعجیل پس از کسر تحمل</param>
/// <param name="WorkedHours">کارکرد مؤثر پس از کسر استراحت</param>
/// <param name="OvertimeHours">اضافه‌کاری پس از آستانه پایان شیفت</param>
/// <param name="Status">وضعیت نهایی روز</param>
public record DayComputation(
    TimeOnly? CheckIn,
    TimeOnly? CheckOut,
    int LateMinutes,
    int EarlyLeaveMinutes,
    decimal WorkedHours,
    decimal OvertimeHours,
    AttendanceStatus Status);

/// <summary>
/// محاسباتگر حضور و غیاب — قلب ماژول: تطبیق ضربه‌های خام با شیفت
/// کاملاً خالص (بدون وابستگی به دیتابیس) تا با تست واحد سناریوساز شود.
/// قواعد:
/// ۱. ضربه‌های همسازه نزدیک‌تر از حد فاصله شیفت حذف می‌شوند (ضربه تصادفی)
/// ۲. پنجره تطبیق از ۴ ساعت پیش از شروع تا ۶ ساعت پس از پایان شیفت است
/// ۳. شیفت شب: پایان پس از نیمه‌شب به همان روزِ شروع تعلق می‌گیرد
/// ۴. تأخیر/تعجیل پس از کسر تحملِ شیفت محاسبه می‌شود
/// ۵. اضافه‌کاری پس از آستانه پایان شیفت آغاز می‌شود
/// </summary>
public static class AttendanceCalculator
{
    /// <summary>حاشیه پنجره تطبیق پیش از شروع شیفت (دقیقه)</summary>
    public const int WindowBeforeMinutes = 240;

    /// <summary>حاشیه پنجره تطبیق پس از پایان شیفت (دقیقه)</summary>
    public const int WindowAfterMinutes = 360;

    /// <summary>حداکثر فاصله ورود تا خروج معتبر — ۱۶ ساعت</summary>
    public const int MaxSpanMinutes = 16 * 60;

    /// <summary>
    /// محاسبه یک روز — ورودی: ضربه‌های UTC مرتب‌نشده، شیفت (اختیاری)، مرخصی تأییدشده و تعطیلی
    /// </summary>
    public static DayComputation Compute(
        DateOnly date,
        IEnumerable<DateTime> punchTimesUtc,
        Shift? shift,
        EmployeeLeave? approvedLeave = null,
        bool isOfficialHoliday = false)
    {
        ArgumentNullException.ThrowIfNull(punchTimesUtc);

        var punches = punchTimesUtc
            .Select(p => DateTime.SpecifyKind(p, DateTimeKind.Utc))
            .OrderBy(p => p)
            .ToList();

        // مرخصی/مأموریت تأییدشدهٔ پوشاننده کل روز، بدون ضربه → وضعیت مرخصی
        if (approvedLeave is not null && CoversFullDay(approvedLeave, date) && punches.Count == 0)
        {
            var leaveStatus = approvedLeave.LeaveType == LeaveType.Mission
                ? AttendanceStatus.Mission
                : AttendanceStatus.Leave;
            return new DayComputation(null, null, 0, 0, 0m, 0m, leaveStatus);
        }

        // بدون شیفت تعریف‌شده — فقط تجمیع ساده اولین/آخرین ضربه
        if (shift is null)
        {
            return ComputeWithoutShift(date, punches);
        }

        // پنجره تطبیق بر پایه تاریخِ محلی شیفت
        var shiftStart = date.ToDateTime(shift.StartTime);
        var shiftEnd = shiftStart.AddMinutes(shift.DurationMinutes);

        var inWindow = punches
            .Where(p => p >= shiftStart.AddMinutes(-WindowBeforeMinutes)
                     && p <= shiftEnd.AddMinutes(WindowAfterMinutes))
            .ToList();

        if (inWindow.Count == 0)
        {
            // تعطیل رسمی بدون پوشش — روز کاری نیست؛ غیبت ثبت نمی‌شود
            if (isOfficialHoliday)
                return new DayComputation(null, null, 0, 0, 0m, 0m, AttendanceStatus.Present);

            return new DayComputation(null, null, 0, 0, 0m, 0m, AttendanceStatus.Absent);
        }

        // حذف ضربه‌های همسازه — فقط اولین ضربه از هر خوشه
        var effective = CollapseClosePunches(inWindow, shift.MinMinutesBetweenPunches);

        var checkIn = effective[0];
        var checkOut = effective[^1];

        // اگر تنها یک ضربه هست و آن هم «ورود» است — خروج هنوز ثبت نشده
        var hasCheckOut = effective.Count > 1;

        // سقف فاصله معتبر — بیشتر از آن به‌عنوان روزِ ناقص حساب می‌شود
        if ((checkOut - checkIn).TotalMinutes > MaxSpanMinutes)
            checkOut = checkIn.AddMinutes(MaxSpanMinutes);

        var lateMinutes = Math.Max(0,
            (int)(checkIn - shiftStart).TotalMinutes - shift.LateGraceMinutes);
        var earlyMinutes = hasCheckOut
            ? Math.Max(0, (int)(shiftEnd - checkOut).TotalMinutes - shift.EarlyLeaveGraceMinutes)
            : 0;

        var workedRaw = Math.Max(0m,
            (decimal)(checkOut - checkIn).TotalMinutes - shift.BreakMinutes) / 60m;
        workedRaw = Math.Min(workedRaw, (decimal)shift.DurationMinutes / 60m);

        var overtimeThreshold = shiftEnd.AddMinutes(shift.OvertimeStartAfterMinutes);
        var overtimeMinutes = Math.Max(0, (int)(checkOut - overtimeThreshold).TotalMinutes);

        var status = lateMinutes > 0
            ? AttendanceStatus.Late
            : AttendanceStatus.Present;

        return new DayComputation(
            TimeOnly.FromDateTime(checkIn),
            hasCheckOut ? TimeOnly.FromDateTime(checkOut) : null,
            lateMinutes,
            earlyMinutes,
            decimal.Round(workedRaw, 2),
            decimal.Round(overtimeMinutes / 60m, 2),
            status);
    }

    /// <summary>تجمیع بدون شیفت — اولین و آخرین ضربه همان روز</summary>
    private static DayComputation ComputeWithoutShift(DateOnly date, List<DateTime> punches)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var sameDay = punches.Where(p => p >= dayStart && p < dayEnd).ToList();
        if (sameDay.Count == 0)
            return new DayComputation(null, null, 0, 0, 0m, 0m, AttendanceStatus.Absent);

        var checkIn = sameDay[0];
        var checkOut = sameDay[^1];
        var hours = sameDay.Count > 1
            ? decimal.Round((decimal)(checkOut - checkIn).TotalMinutes / 60m, 2)
            : 0m;

        return new DayComputation(
            TimeOnly.FromDateTime(checkIn),
            sameDay.Count > 1 ? TimeOnly.FromDateTime(checkOut) : null,
            0, 0, hours, 0m, AttendanceStatus.Present);
    }

    /// <summary>حذف ضربه‌های همسازه — از هر خوشه فقط ضربه نخست باقی می‌ماند</summary>
    private static List<DateTime> CollapseClosePunches(List<DateTime> ordered, int minGapMinutes)
    {
        if (minGapMinutes <= 0 || ordered.Count <= 1)
            return ordered;

        var result = new List<DateTime> { ordered[0] };
        foreach (var p in ordered.Skip(1))
        {
            if ((p - result[^1]).TotalMinutes >= minGapMinutes)
                result.Add(p);
        }
        return result;
    }

    /// <summary>آیا مرخصی همه روز داده‌شده را می‌پوشاند؟</summary>
    private static bool CoversFullDay(EmployeeLeave leave, DateOnly date) =>
        leave.Status == LeaveStatus.Approved
        && leave.FromDate is { } from && leave.ToDate is { } to
        && date >= from && date <= to;
}
