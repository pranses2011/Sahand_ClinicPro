namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع قالب چاپ — ۹ نوع سند چاپی درمانگاه
/// </summary>
public enum PrintTemplateType : byte
{
    /// <summary>فاکتور و قبض پرداخت</summary>
    Invoice = 1,

    /// <summary>نسخه پزشکی</summary>
    Prescription = 2,

    /// <summary>درخواست آزمایش</summary>
    LabOrder = 3,

    /// <summary>نتیجه آزمایش</summary>
    LabResult = 4,

    /// <summary>درخواست تصویربرداری</summary>
    ImagingOrder = 5,

    /// <summary>نوبت‌نامه</summary>
    AppointmentTicket = 6,

    /// <summary>رسید پذیرش</summary>
    ReceptionReceipt = 7,

    /// <summary>گواهی پزشکی</summary>
    MedicalCertificate = 8,

    /// <summary>فیش حقوقی</summary>
    PayrollSlip = 9
}
