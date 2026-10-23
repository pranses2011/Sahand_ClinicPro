namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>قواعد زمانی برنامهٔ کاری پزشک، شامل شیفت‌های عبوری از نیمه‌شب.</summary>
public static class DoctorScheduleTime
{
    /// <summary>ساعت پایان کوچک‌تر از شروع یعنی شیفت تا روز بعد ادامه دارد.</summary>
    public static bool CrossesMidnight(TimeOnly start, TimeOnly end) => end < start;

    /// <summary>شروع و پایان برابر بازهٔ صفر و نامعتبر است.</summary>
    public static bool IsValidRange(TimeOnly start, TimeOnly end) => start != end;

    /// <summary>مدت شیفت بر حسب دقیقه، با احتساب عبور از نیمه‌شب.</summary>
    public static int DurationMinutes(TimeOnly start, TimeOnly end)
    {
        var minutes = (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;
        return minutes < 0 ? minutes + 24 * 60 : minutes;
    }
}
