namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// کد تشخیص ICD-10 - طبقه‌بندی بین‌المللی بیماری‌ها
/// </summary>
public class Icd10Code : AuditableEntity, ISoftDeletable
{
    /// <summary>کد ICD-10 مثل E11 - یکتا</summary>
    public string Code { get; set; } = null!;

    /// <summary>شرح انگلیسی تشخیص</summary>
    public string Description { get; set; } = null!;

    /// <summary>شرح فارسی تشخیص</summary>
    public string? DescriptionFa { get; set; }

    /// <summary>کد والد در طبقه‌بندی</summary>
    public string? ParentCode { get; set; }

    /// <summary>دسته‌بندی</summary>
    public string? Category { get; set; }

    /// <summary>فصل ICD-10</summary>
    public string? Chapter { get; set; }

    /// <summary>بلوک ICD-10</summary>
    public string? Block { get; set; }

    /// <summary>ویزیت‌هایی با این تشخیص اصلی</summary>
    public ICollection<Visit> PrimaryVisits { get; set; } = new List<Visit>();
}
