namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// تخصص پزشکی - درختی با ارجاع به والد برای فوق‌تخصص‌ها
/// </summary>
public class Specialty : AuditableEntity, ISoftDeletable
{
    /// <summary>نام تخصص به فارسی</summary>
    public string Name { get; set; } = null!;

    /// <summary>نام تخصص به انگلیسی</summary>
    public string? NameEn { get; set; }

    /// <summary>کد یکتای تخصص</summary>
    public string? Code { get; set; }

    /// <summary>شناسه تخصص والد - برای زیرتخصص</summary>
    public long? ParentId { get; set; }

    /// <summary>توضیحات</summary>
    public string? Description { get; set; }

    /// <summary>مسیر آیکون</summary>
    public string? IconPath { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>تخصص والد</summary>
    public Specialty? Parent { get; set; }

    /// <summary>زیرتخصص‌ها</summary>
    public ICollection<Specialty> Children { get; set; } = new List<Specialty>();

    /// <summary>پزشکان این تخصص</summary>
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
