namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع فاکتور
/// </summary>
public enum InvoiceType : byte
{
    /// <summary>ویزیت</summary>
    Visit = 1,
    /// <summary>آزمایشگاه</summary>
    Lab = 2,
    /// <summary>تصویربرداری</summary>
    Imaging = 3,
    /// <summary>داروخانه</summary>
    Pharmacy = 4,
    /// <summary>دندانپزشکی</summary>
    Dental = 5,
    /// <summary>فیزیوتراپی</summary>
    Physiotherapy = 6,
    /// <summary>سایر</summary>
    Other = 7,
}
