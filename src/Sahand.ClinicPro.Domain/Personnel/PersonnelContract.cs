using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// قرارداد پرسنلی - سند رسمی همکاری
/// </summary>
public class PersonnelContract : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه کارمند</summary>
    public long EmployeeId { get; set; }

    /// <summary>شماره قرارداد - یکتا</summary>
    public string? ContractNumber { get; set; }

    /// <summary>نوع قرارداد</summary>
    public ContractType ContractType { get; set; }

    /// <summary>شروع قرارداد</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>پایان قرارداد</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>حقوق قراردادی</summary>
    public decimal Salary { get; set; } = 0m;

    /// <summary>شرایط قرارداد</summary>
    public string? Terms { get; set; }

    /// <summary>مسیر فایل قرارداد</summary>
    public string? DocumentPath { get; set; }

    /// <summary>وضعیت</summary>
    public ContractStatus Status { get; set; }

    /// <summary>کارمند</summary>
    public Employee? Employee { get; set; }
}
