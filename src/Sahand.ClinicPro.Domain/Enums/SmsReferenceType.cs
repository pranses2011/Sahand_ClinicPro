namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// منشأ پیامک
/// </summary>
public enum SmsReferenceType : byte
{
    /// <summary>نوبت</summary>
    Appointment = 1,
    /// <summary>ویزیت</summary>
    Visit = 2,
    /// <summary>آزمایش</summary>
    LabOrder = 3,
    /// <summary>فاکتور</summary>
    Invoice = 4,
    /// <summary>دستی</summary>
    Manual = 5,
}
