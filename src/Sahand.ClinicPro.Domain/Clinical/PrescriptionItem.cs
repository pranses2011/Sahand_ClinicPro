namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// قلم نسخه - یک دارو با دستور مصرف
/// </summary>
public class PrescriptionItem : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه نسخه</summary>
    public long PrescriptionId { get; set; }

    /// <summary>شناسه دارو</summary>
    public long MedicationId { get; set; }

    /// <summary>ترتیب در نسخه</summary>
    public int? OrderIndex { get; set; }

    /// <summary>دوز مصرف مثل یک قرص</summary>
    public string? Dosage { get; set; }

    /// <summary>تعداد مصرف در روز</summary>
    public string? Frequency { get; set; }

    /// <summary>مدت مصرف مثل هفت روز</summary>
    public string? Duration { get; set; }

    /// <summary>مسیر مصرف - خوراکی یا تزریقی</summary>
    public string? Route { get; set; }

    /// <summary>تعداد کل</summary>
    public decimal Quantity { get; set; } = 0m;

    /// <summary>واحد شمارش مثل عدد یا آمپول</summary>
    public string? Unit { get; set; }

    /// <summary>دستورات خاص مصرف</summary>
    public string? Instructions { get; set; }

    /// <summary>مصرف در صورت نیاز</summary>
    public bool IsSos { get; set; } = false;

    /// <summary>از داروخانه تحویل شده</summary>
    public bool IsDelivered { get; set; } = false;

    /// <summary>تاریخ تحویل</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>تعداد تحویل‌شده</summary>
    public decimal DeliveredQuantity { get; set; } = 0m;

    /// <summary>نسخه والد</summary>
    public Prescription? Prescription { get; set; }

    /// <summary>داروی تجویزشده</summary>
    public Medication? Medication { get; set; }
}
