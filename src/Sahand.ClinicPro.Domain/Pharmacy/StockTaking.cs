using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// انبارگردانی - شمارش ادواری موجودی
/// گام P2 (چرخه ششم): چرخه عملیاتی سه‌مرحله‌ای — در حال شمارش ← تکمیل ← تأیید و اعمال مغایرت
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

    /// <summary>شماره جلسه انبارگردانی — برای ارجاع انسانی</summary>
    public string? TakingNumber { get; set; }

    /// <summary>تاریخ تأیید نهایی و اعمال مغایرت</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>اقلام شمارش این جلسه — عکس فوری از موجودی هنگام شروع</summary>
    public ICollection<StockTakingItem> Items { get; set; } = new List<StockTakingItem>();
}
