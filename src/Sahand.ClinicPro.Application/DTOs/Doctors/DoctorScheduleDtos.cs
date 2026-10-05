using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Doctors;

/// <summary>
/// ردیف برنامه هفتگی پزشک — هفت روز هفته ایرانی
/// DayOfWeek: شنبه=۰ ... جمعه=۶
/// </summary>
public class ScheduleRowDto
{
    /// <summary>روز هفته — شنبه=۰ تا جمعه=۶</summary>
    public byte DayOfWeek { get; set; }

    /// <summary>روز فعال است</summary>
    public bool IsEnabled { get; set; }

    /// <summary>ساعت شروع — قالب HH:mm</summary>
    public string? StartTime { get; set; }

    /// <summary>ساعت پایان — قالب HH:mm</summary>
    public string? EndTime { get; set; }

    /// <summary>شروع استراحت — اختیاری</summary>
    public string? BreakStart { get; set; }

    /// <summary>پایان استراحت — اختیاری</summary>
    public string? BreakEnd { get; set; }

    /// <summary>شناسه اتاق ویزیت</summary>
    public long? RoomId { get; set; }

    /// <summary>نام اتاق برای نمایش</summary>
    public string? RoomName { get; set; }

    /// <summary>حداکثر ظرفیت بیمار</summary>
    public int? MaxPatients { get; set; }
}

/// <summary>
/// درخواست ذخیره برنامه هفتگی — جایگزینی کامل هفت ردیف
/// </summary>
public class SaveScheduleRequest
{
    /// <summary>ردیف‌های هفت روز</summary>
    public List<ScheduleRowDto> Rows { get; set; } = new();
}

/// <summary>
/// غیبت ثبت‌شده پزشک
/// </summary>
public class AbsenceDto
{
    /// <summary>شناسه غیبت</summary>
    public long Id { get; set; }

    /// <summary>تاریخ غیبت</summary>
    public string DateShamsi { get; set; } = string.Empty;

    /// <summary>از ساعت — خالی یعنی کل روز</summary>
    public string? FromTime { get; set; }

    /// <summary>تا ساعت</summary>
    public string? ToTime { get; set; }

    /// <summary>نوع غیبت</summary>
    public string AbsenceTypeName { get; set; } = string.Empty;

    /// <summary>دلیل</summary>
    public string? Reason { get; set; }

    /// <summary>تعداد نوبت‌های رزروشده در بازه — برای هشدار</summary>
    public int BookedAppointments { get; set; }
}

/// <summary>
/// درخواست ثبت غیبت جدید
/// </summary>
public class CreateAbsenceRequest
{
    /// <summary>تاریخ شمسی — ۱۴۰۴/۰۴/۰۳</summary>
    public string DateShamsi { get; set; } = string.Empty;

    /// <summary>از ساعت — اختیاری (خالی = کل روز)</summary>
    public string? FromTime { get; set; }

    /// <summary>تا ساعت — اختیاری</summary>
    public string? ToTime { get; set; }

    /// <summary>نوع غیبت</summary>
    public AbsenceType AbsenceType { get; set; }

    /// <summary>دلیل</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// آمار عملکرد پزشک — برای دکمه آمار در فهرست
/// </summary>
public class DoctorPerformanceDto
{
    /// <summary>تعداد ویزیت این ماه</summary>
    public int VisitsThisMonth { get; set; }

    /// <summary>تعداد ویزیت امروز</summary>
    public int VisitsToday { get; set; }

    /// <summary>درآمد تولیدشده این ماه به ریال</summary>
    public decimal RevenueThisMonth { get; set; }

    /// <summary>کارانه پزشک این ماه — درصد سهم × درآمد</summary>
    public decimal CommissionThisMonth { get; set; }

    /// <summary>میانگین مدت ویزیت به دقیقه</summary>
    public double AverageVisitMinutes { get; set; }

    /// <summary>امتیاز رضایت از ۵</summary>
    public decimal Rating { get; set; }

    /// <summary>نرخ عدم مراجعه به درصد</summary>
    public double NoShowRate { get; set; }
}
