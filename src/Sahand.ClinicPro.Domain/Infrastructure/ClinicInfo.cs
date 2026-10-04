namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// اطلاعات درمانگاه - مشخصات هویتی برای سربرگ چاپ
/// </summary>
public class ClinicInfo : AuditableEntity, ISoftDeletable
{
    /// <summary>نام درمانگاه</summary>
    public string ClinicName { get; set; } = null!;

    /// <summary>نام انگلیسی</summary>
    public string? ClinicNameEn { get; set; }

    /// <summary>مسیر لوگو</summary>
    public string? LogoPath { get; set; }

    /// <summary>شماره مجوز</summary>
    public string? LicenseNumber { get; set; }

    /// <summary>کد اقتصادی</summary>
    public string? EconomicCode { get; set; }

    /// <summary>شناسه ملی</summary>
    public string? NationalId { get; set; }

    /// <summary>نشانی</summary>
    public string? Address { get; set; }

    /// <summary>تلفن</summary>
    public string? Phone { get; set; }

    /// <summary>فکس</summary>
    public string? Fax { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>وب‌سایت</summary>
    public string? Website { get; set; }

    /// <summary>نام مدیر</summary>
    public string? ManagerName { get; set; }

    /// <summary>تاریخ تأسیس</summary>
    public DateOnly? FoundationDate { get; set; }
}
