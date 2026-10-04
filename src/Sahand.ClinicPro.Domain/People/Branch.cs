namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// شعبه درمانگاه - زیرساخت چندشعبه‌ای برای آینده
/// </summary>
public class Branch : AuditableEntity, ISoftDeletable
{
    /// <summary>نام شعبه</summary>
    public string BranchName { get; set; } = null!;

    /// <summary>کد یکتای شعبه</summary>
    public string? BranchCode { get; set; }

    /// <summary>آدرس</summary>
    public string? Address { get; set; }

    /// <summary>شهر</summary>
    public string? City { get; set; }

    /// <summary>استان</summary>
    public string? Province { get; set; }

    /// <summary>تلفن</summary>
    public string? Phone { get; set; }

    /// <summary>شعبه اصلی</summary>
    public bool IsMain { get; set; } = false;

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;
}
