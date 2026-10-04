namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// آزمایش - کاتالوگ تست‌های آزمایشگاهی
/// </summary>
public class LabTest : AuditableEntity, ISoftDeletable
{
    /// <summary>کد تست - یکتا</summary>
    public string? TestCode { get; set; }

    /// <summary>نام آزمایش</summary>
    public string TestName { get; set; } = null!;

    /// <summary>نام انگلیسی</summary>
    public string? TestNameEn { get; set; }

    /// <summary>دسته آزمایش مثل بیوشیمی</summary>
    public string? Category { get; set; }

    /// <summary>نوع نمونه مثل خون یا ادرار</summary>
    public string? SampleType { get; set; }

    /// <summary>نوع ظرف نمونه</summary>
    public string? ContainerType { get; set; }

    /// <summary>حد طبیعی آقایان</summary>
    public string? NormalRangeMale { get; set; }

    /// <summary>حد طبیعی خانم‌ها</summary>
    public string? NormalRangeFemale { get; set; }

    /// <summary>واحد سنجش</summary>
    public string? Unit { get; set; }

    /// <summary>قیمت</summary>
    public decimal Price { get; set; } = 0m;

    /// <summary>زمان جواب به ساعت</summary>
    public int? TurnAroundTime { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>اقلام درخواست با این تست</summary>
    public ICollection<LabOrderItem> OrderItems { get; set; } = new List<LabOrderItem>();
}
