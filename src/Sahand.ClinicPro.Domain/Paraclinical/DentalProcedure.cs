namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// عمل دندانپزشکی - کاتالوگ اعمال دندانپزشکی
/// </summary>
public class DentalProcedure : AuditableEntity, ISoftDeletable
{
    /// <summary>نام عمل</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتا</summary>
    public string? Code { get; set; }

    /// <summary>دسته</summary>
    public string? Category { get; set; }

    /// <summary>قیمت پیش‌فرض</summary>
    public decimal DefaultPrice { get; set; } = 0m;

    /// <summary>مدت به دقیقه</summary>
    public int? Duration { get; set; }

    /// <summary>رکوردهای این عمل</summary>
    public ICollection<DentalRecord> Records { get; set; } = new List<DentalRecord>();
}
