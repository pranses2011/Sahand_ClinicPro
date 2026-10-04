using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// رکورد دندانپزشکی - وضعیت هر دندان با شماره‌گذاری FDI
/// </summary>
public class DentalRecord : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه ویزیت</summary>
    public long? VisitId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه دندانپزشک</summary>
    public long? DentistId { get; set; }

    /// <summary>شماره دندان - سیستم FDI</summary>
    public int ToothNumber { get; set; }

    /// <summary>سطح دندان مثل M یا O</summary>
    public string? Surface { get; set; }

    /// <summary>وضعیت دندان</summary>
    public DentalCondition Condition { get; set; }

    /// <summary>شناسه عمل انجام‌شده</summary>
    public long? ProcedureId { get; set; }

    /// <summary>تاریخ عمل</summary>
    public DateOnly? ProcedureDate { get; set; }

    /// <summary>مواد مصرفی</summary>
    public string? MaterialUsed { get; set; }

    /// <summary>یادداشت</summary>
    public string? Notes { get; set; }

    /// <summary>هزینه</summary>
    public decimal Price { get; set; } = 0m;

    /// <summary>وضعیت</summary>
    public TreatmentPlanStatus Status { get; set; }

    /// <summary>ویزیت مرتبط</summary>
    public Visit? Visit { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>عمل مرتبط</summary>
    public DentalProcedure? Procedure { get; set; }
}
