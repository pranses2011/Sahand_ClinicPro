using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// قلم آزمایش - نتیجه یک تست خاص
/// </summary>
public class LabOrderItem : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه درخواست</summary>
    public long LabOrderId { get; set; }

    /// <summary>شناسه تست</summary>
    public long TestId { get; set; }

    /// <summary>نتیجه متنی</summary>
    public string? Result { get; set; }

    /// <summary>نتیجه عددی</summary>
    public decimal? ResultNumeric { get; set; } = 0m;

    /// <summary>حد طبیعی</summary>
    public string? NormalRange { get; set; }

    /// <summary>واحد</summary>
    public string? Unit { get; set; }

    /// <summary>خارج از حد طبیعی</summary>
    public bool IsAbnormal { get; set; } = false;

    /// <summary>پرچم غیرطبیعی مثل H یا L</summary>
    public string? AbnormalFlag { get; set; }

    /// <summary>توضیح آزمایشگاه</summary>
    public string? Comment { get; set; }

    /// <summary>وضعیت قلم</summary>
    public LabItemStatus Status { get; set; } = Domain.Enums.LabItemStatus.Pending;

    /// <summary>درخواست والد</summary>
    public LabOrder? LabOrder { get; set; }

    /// <summary>تست مربوطه</summary>
    public LabTest? Test { get; set; }
}
