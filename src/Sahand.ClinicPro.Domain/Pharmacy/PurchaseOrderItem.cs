namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// قلم سفارش خرید - یک دارو با مقدار
/// </summary>
public class PurchaseOrderItem : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه سفارش</summary>
    public long OrderId { get; set; }

    /// <summary>شناسه دارو</summary>
    public long MedicationId { get; set; }

    /// <summary>تعداد سفارش</summary>
    public decimal Quantity { get; set; } = 0m;

    /// <summary>قیمت واحد</summary>
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>قیمت کل</summary>
    public decimal TotalPrice { get; set; } = 0m;

    /// <summary>تعداد دریافتی</summary>
    public decimal ReceivedQuantity { get; set; } = 0m;

    /// <summary>سفارش والد</summary>
    public PurchaseOrder? Order { get; set; }

    /// <summary>دارو</summary>
    public Medication? Medication { get; set; }
}
