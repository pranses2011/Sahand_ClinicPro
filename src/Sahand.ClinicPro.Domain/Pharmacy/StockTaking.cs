using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// انبارگردانی - شمارش ادواری موجودی
/// </summary>
public class StockTaking : AuditableEntity, ISoftDeletable
{
    /// <summary>تاریخ انبارگردانی</summary>
    public DateOnly? TakingDate { get; set; }

    /// <summary>شناسه انجام‌دهنده</summary>
    public long? PerformedBy { get; set; }

    /// <summary>وضعیت</summary>
    public StockTakingStatus Status { get; set; }

    /// <summary>توضیحات</summary>
    public string? Notes { get; set; }
}
