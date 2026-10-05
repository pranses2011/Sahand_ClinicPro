namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// اندازه‌گیری رشد کودک — وزن/قد/محیط سر برای نمودار رشد WHO — گام R2
/// هر رکورد یک نوبت اندازه‌گیری است؛ z-score در سمت کلاینت از داده مرجع WHO محاسبه می‌شود
/// </summary>
public class GrowthMeasurement : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>تاریخ اندازه‌گیری (میلادی — بدون ساعت)</summary>
    public DateOnly MeasuredAt { get; set; }

    /// <summary>سن هنگام اندازه‌گیری به ماه (بر اساس تاریخ تولد بیمار)</summary>
    public int AgeMonths { get; set; }

    /// <summary>وزن به کیلوگرم (دقت ۱۰۰ گرم)</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>قد/طول به سانتی‌متر (دقت ۰/۵)</summary>
    public decimal? HeightCm { get; set; }

    /// <summary>محیط سر به سانتی‌متر — تا ۳۶ ماهگی معیار مهم است</summary>
    public decimal? HeadCircumferenceCm { get; set; }

    /// <summary>یادداشت شرایط اندازه‌گیری (ناشتا/با لباس و...)</summary>
    public string? Notes { get; set; }

    /// <summary>بیمار والد</summary>
    public People.Patient? Patient { get; set; }
}
