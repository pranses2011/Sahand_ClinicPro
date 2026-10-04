using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Integration;

/// <summary>
/// تراکنش سپاس - ثبت ویزیت در سامانه پوشش سلامت همگا
/// </summary>
public class SepasTransaction : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه ویزیت</summary>
    public long? VisitId { get; set; }

    /// <summary>نوع تراکنش</summary>
    public SepasTransactionType TransactionType { get; set; }

    /// <summary>شناسه ارجاع سپاس</summary>
    public string? SepasReferenceId { get; set; }

    /// <summary>درخواست خام - JSON</summary>
    public string? RequestData { get; set; }

    /// <summary>پاسخ خام - JSON</summary>
    public string? ResponseData { get; set; }

    /// <summary>وضعیت همگام‌سازی</summary>
    public SyncStatus Status { get; set; }

    /// <summary>خطای احتمالی</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>زمان ارسال</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>زمان پاسخ</summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>تعداد بازتلاش</summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }
}
