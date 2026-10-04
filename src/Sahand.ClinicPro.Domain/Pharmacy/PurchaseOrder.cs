using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// سفارش خرید دارو - از تأمین‌کننده
/// </summary>
public class PurchaseOrder : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره سفارش - یکتا</summary>
    public string? OrderNumber { get; set; }

    /// <summary>شناسه تأمین‌کننده</summary>
    public long SupplierId { get; set; }

    /// <summary>تاریخ سفارش</summary>
    public DateOnly? OrderDate { get; set; }

    /// <summary>تاریخ تحویل مورد انتظار</summary>
    public DateOnly? ExpectedDate { get; set; }

    /// <summary>تاریخ تحویل واقعی</summary>
    public DateOnly? ReceivedDate { get; set; }

    /// <summary>مبلغ کل</summary>
    public decimal TotalAmount { get; set; } = 0m;

    /// <summary>وضعیت سفارش</summary>
    public PurchaseOrderStatus Status { get; set; }

    /// <summary>تأمین‌کننده</summary>
    public Supplier? Supplier { get; set; }

    /// <summary>اقلام سفارش</summary>
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
