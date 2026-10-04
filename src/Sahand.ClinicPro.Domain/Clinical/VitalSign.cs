namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// علائم حیاتی - اندازه‌گیری‌های بالینی در جریان ویزیت
/// </summary>
public class VitalSign : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه ویزیت</summary>
    public long VisitId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>زمان اندازه‌گیری</summary>
    public DateTime? MeasuredAt { get; set; }

    /// <summary>دمای بدن به سلسیوس</summary>
    public decimal? Temperature { get; set; } = 0m;

    /// <summary>فشار خون سیستولیک به میلی‌متر جیوه</summary>
    public int? BloodPressureSystolic { get; set; }

    /// <summary>فشار خون دیاستولیک</summary>
    public int? BloodPressureDiastolic { get; set; }

    /// <summary>ضربان قلب در دقیقه</summary>
    public int? HeartRate { get; set; }

    /// <summary>تعداد تنفس در دقیقه</summary>
    public int? RespiratoryRate { get; set; }

    /// <summary>اشباع اکسیژن خون به درصد</summary>
    public int? OxygenSaturation { get; set; }

    /// <summary>وزن به کیلوگرم</summary>
    public decimal? Weight { get; set; } = 0m;

    /// <summary>قد به سانتی‌متر</summary>
    public decimal? Height { get; set; } = 0m;

    /// <summary>شاخص توده بدنی - محاسباتی</summary>
    public decimal? Bmi { get; set; } = 0m;

    /// <summary>قند خون به میلی‌گرم بر دسی‌لیتر</summary>
    public int? BloodSugar { get; set; }

    /// <summary>دور سر به سانتی‌متر - برای نوزاد</summary>
    public decimal? HeadCircumference { get; set; } = 0m;

    /// <summary>شناسه کاربر اندازه‌گیری‌کننده</summary>
    public long? MeasuredBy { get; set; }

    /// <summary>ویزیت مرتبط</summary>
    public Visit? Visit { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }
}
