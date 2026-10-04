namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// منشأ سند/فایل
/// </summary>
public enum DocumentReferenceType : byte
{
    /// <summary>بیمار</summary>
    Patient = 1,
    /// <summary>ویزیت</summary>
    Visit = 2,
    /// <summary>آزمایش</summary>
    LabOrder = 3,
    /// <summary>تصویربرداری</summary>
    ImagingOrder = 4,
    /// <summary>فاکتور</summary>
    Invoice = 5,
    /// <summary>کارمند</summary>
    Employee = 6,
    /// <summary>سایر</summary>
    Other = 7,
}
