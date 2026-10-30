namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// معاینه چشم - تجویز عینک و اندازه‌های چشم
/// </summary>
public class EyeExamRecord : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه ویزیت</summary>
    public long? VisitId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>عدسی چشم راست</summary>
    public decimal? RightSphere { get; set; } = 0m;

    /// <summary>استوانه راست</summary>
    public decimal? RightCylinder { get; set; } = 0m;

    /// <summary>محور راست</summary>
    public int? RightAxis { get; set; }

    /// <summary>عدسی چپ</summary>
    public decimal? LeftSphere { get; set; } = 0m;

    /// <summary>استوانه چپ</summary>
    public decimal? LeftCylinder { get; set; } = 0m;

    /// <summary>محور چپ</summary>
    public int? LeftAxis { get; set; }

    /// <summary>حدت بینایی راست</summary>
    public string? RightVisualAcuity { get; set; }

    /// <summary>حدت بینایی چپ</summary>
    public string? LeftVisualAcuity { get; set; }

    /// <summary>حدت بینایی راست با اصلاح (عینک/لنز)؛ سابقهٔ قدیمی ممکن است تهی باشد.</summary>
    public string? RightCorrectedVisualAcuity { get; set; }

    /// <summary>حدت بینایی چپ با اصلاح (عینک/لنز).</summary>
    public string? LeftCorrectedVisualAcuity { get; set; }

    /// <summary>فشار داخل چشم راست</summary>
    public int? RightIop { get; set; }

    /// <summary>فشار داخل چشم چپ</summary>
    public int? LeftIop { get; set; }

    /// <summary>فاصقه مردمک</summary>
    public decimal? PupilDistance { get; set; } = 0m;

    /// <summary>تست تشخیص رنگ</summary>
    public string? ColorVision { get; set; }

    /// <summary>میدان بینایی</summary>
    public string? VisualField { get; set; }

    /// <summary>یافته‌ها</summary>
    public string? Findings { get; set; }

    /// <summary>تجویز عینک</summary>
    public string? EyewearPrescription { get; set; }

    /// <summary>ویزیت مرتبط</summary>
    public Visit? Visit { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }
}
