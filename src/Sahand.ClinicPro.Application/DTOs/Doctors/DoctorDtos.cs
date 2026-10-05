using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Doctors;

/// <summary>
/// آیتم فهرست پزشکان
/// </summary>
public class DoctorListDto
{
    /// <summary>شناسه پزشک</summary>
    public long Id { get; set; }

    /// <summary>نام کامل</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>کد نظام پزشکی</summary>
    public string? NezamPezeshkiCode { get; set; }

    /// <summary>نام تخصص</summary>
    public string? SpecialtyName { get; set; }

    /// <summary>حق ویزیت به ریال</summary>
    public decimal ConsultationFee { get; set; }

    /// <summary>مدت هر ویزیت به دقیقه</summary>
    public int VisitDuration { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// جزئیات پزشک
/// </summary>
public class DoctorDetailDto
{
    /// <summary>شناسه پزشک</summary>
    public long Id { get; set; }

    /// <summary>شناسه پرسنل مرتبط</summary>
    public long? EmployeeId { get; set; }

    /// <summary>شماره پروانه پزشکی</summary>
    public string? MedicalLicenseNo { get; set; }

    /// <summary>کد نظام پزشکی</summary>
    public string? NezamPezeshkiCode { get; set; }

    /// <summary>تخصص اصلی — شناسه</summary>
    public long? SpecialtyId { get; set; }

    /// <summary>نام تخصص اصلی</summary>
    public string? SpecialtyName { get; set; }

    /// <summary>فوق‌تخصص — شناسه</summary>
    public long? SubSpecialtyId { get; set; }

    /// <summary>مرتبه علمی</summary>
    public string? AcademicRank { get; set; }

    /// <summary>بیوگرافی</summary>
    public string? Biography { get; set; }

    /// <summary>حق ویزیت به ریال</summary>
    public decimal ConsultationFee { get; set; }

    /// <summary>مدت هر ویزیت به دقیقه</summary>
    public int VisitDuration { get; set; }

    /// <summary>حداکثر بیمار در هر شیفت</summary>
    public int? MaxPatientsPerShift { get; set; }

    /// <summary>درصد سهم پزشک</summary>
    public decimal DoctorPercentage { get; set; }

    /// <summary>نوبت‌دهی آنلاین فعال</summary>
    public bool OnlineBookingEnabled { get; set; }

    /// <summary>پذیرش بیمار جدید</summary>
    public bool AcceptsNewPatients { get; set; }

    /// <summary>تصویر امضا — Base64</summary>
    public string? SignatureImageBase64 { get; set; }

    /// <summary>تصویر مهر — Base64</summary>
    public string? StampImageBase64 { get; set; }


    /// <summary>امتیاز</summary>
    public decimal Rating { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// درخواست ثبت یا ویرایش پزشک
/// </summary>
public class SaveDoctorDto
{
    /// <summary>شناسه پرسنل مرتبط</summary>
    public long? EmployeeId { get; set; }

    /// <summary>شماره پروانه پزشکی</summary>
    public string? MedicalLicenseNo { get; set; }

    /// <summary>کد نظام پزشکی</summary>
    public string? NezamPezeshkiCode { get; set; }

    /// <summary>تخصص اصلی</summary>
    public long? SpecialtyId { get; set; }

    /// <summary>فوق‌تخصص</summary>
    public long? SubSpecialtyId { get; set; }

    /// <summary>مرتبه علمی</summary>
    public string? AcademicRank { get; set; }

    /// <summary>بیوگرافی</summary>
    public string? Biography { get; set; }

    /// <summary>حق ویزیت به ریال</summary>
    public decimal ConsultationFee { get; set; }

    /// <summary>مدت هر ویزیت به دقیقه</summary>
    public int VisitDuration { get; set; } = 15;

    /// <summary>حداکثر بیمار در هر شیفت</summary>
    public int? MaxPatientsPerShift { get; set; }

    /// <summary>درصد سهم پزشک</summary>
    public decimal DoctorPercentage { get; set; }

    /// <summary>نوبت‌دهی آنلاین فعال</summary>
    public bool OnlineBookingEnabled { get; set; } = true;

    /// <summary>پذیرش بیمار جدید</summary>
    public bool AcceptsNewPatients { get; set; } = true;

    /// <summary>تصویر امضا — Base64 از PNG شفاف ۳۰۰×۱۰۰</summary>
    public string? SignatureImageBase64 { get; set; }

    /// <summary>تصویر مهر — Base64 از PNG ۱۵۰×۱۵۰</summary>
    public string? StampImageBase64 { get; set; }

    /// <summary>فعال بودن پزشک</summary>
    public bool IsActive { get; set; } = true;
}
