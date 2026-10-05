using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Patients;

/// <summary>
/// درخواست ثبت بیمار جدید
/// شماره پرونده به‌صورت خودکار با سکانس MRN تولید می‌شود
/// </summary>
public class CreatePatientDto
{
    /// <summary>کد ملی ۱۰ رقمی — یکتا و اعتبارسنجی‌شده با رقم کنترلی</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>نام پدر</summary>
    public string? FatherName { get; set; }

    /// <summary>جنسیت</summary>
    public Gender Gender { get; set; }

    /// <summary>تاریخ تولد شمسی — فرمت 1358/04/15</summary>
    public string? BirthDateShamsi { get; set; }

    /// <summary>گروه خونی</summary>
    public string? BloodType { get; set; }

    /// <summary>موبایل — الگوی 09XXXXXXXXX</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>تلفن ثابت</summary>
    public string? Phone { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>آدرس — با AES-256 رمزنگاری می‌شود</summary>
    public string? Address { get; set; }

    /// <summary>شهر</summary>
    public string? City { get; set; }

    /// <summary>استان</summary>
    public string? Province { get; set; }

    /// <summary>بیمه اصلی</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>شماره دفترچه بیمه</summary>
    public string? InsuranceNumber { get; set; }

    /// <summary>بیمه تکمیلی</summary>
    public long? SupplementaryInsuranceId { get; set; }

    /// <summary>شغل</summary>
    public string? Occupation { get; set; }
}

/// <summary>
/// درخواست ویرایش بیمار
/// </summary>
public class UpdatePatientDto
{
    /// <summary>کد ملی ۱۰ رقمی — با الگوریتم رقم کنترلی اعتبارسنجی می‌شود (ضد جعل)</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>نام پدر</summary>
    public string? FatherName { get; set; }

    /// <summary>موبایل</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>تلفن ثابت</summary>
    public string? Phone { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>آدرس</summary>
    public string? Address { get; set; }

    /// <summary>شهر</summary>
    public string? City { get; set; }

    /// <summary>استان</summary>
    public string? Province { get; set; }

    /// <summary>بیمه اصلی</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>شماره دفترچه بیمه</summary>
    public string? InsuranceNumber { get; set; }

    /// <summary>بیمه تکمیلی</summary>
    public long? SupplementaryInsuranceId { get; set; }
}
