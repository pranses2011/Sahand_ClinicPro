namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع خدمت
/// </summary>
public enum ServiceType : byte
{
    /// <summary>ویزیت</summary>
    Visit = 1,
    /// <summary>آزمایش</summary>
    Lab = 2,
    /// <summary>تصویربرداری</summary>
    Imaging = 3,
    /// <summary>عمل</summary>
    Procedure = 4,
    /// <summary>مشاوره</summary>
    Consultation = 5,
    /// <summary>سایر</summary>
    Other = 6,
}
