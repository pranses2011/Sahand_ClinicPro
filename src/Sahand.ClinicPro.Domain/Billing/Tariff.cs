namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// تعرفه - قیمت سالانه خدمات برای هر بیمه یا آزاد
/// </summary>
public class Tariff : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه خدمت</summary>
    public long ServiceId { get; set; }

    /// <summary>شناسه بیمه - تهی یعنی تعرفه آزاد</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>سال تعرفه</summary>
    public int Year { get; set; }

    /// <summary>قیمت تعرفه‌ای</summary>
    public decimal Price { get; set; } = 0m;

    /// <summary>درصد سهم بیمه</summary>
    public decimal InsuranceShare { get; set; } = 0m;

    /// <summary>درصد سهم بیمار</summary>
    public decimal PatientShare { get; set; } = 0m;

    /// <summary>سقف بیمه</summary>
    public decimal InsuranceCeiling { get; set; } = 0m;

    /// <summary>قیمت بر مبنای K</summary>
    public bool IsK { get; set; } = false;

    /// <summary>مقدار K</summary>
    public decimal KValue { get; set; } = 0m;

    /// <summary>معتبر از</summary>
    public DateOnly? ValidFrom { get; set; }

    /// <summary>معتبر تا</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>خدمت</summary>
    public Service? Service { get; set; }

    /// <summary>بیمه مربوطه</summary>
    public InsuranceCompany? InsuranceCompany { get; set; }
}
