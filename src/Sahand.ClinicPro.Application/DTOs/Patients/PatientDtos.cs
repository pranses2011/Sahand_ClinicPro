using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Patients;

/// <summary>
/// آیتم فهرست بیماران — سبک برای شبکه و گرید WPF
/// </summary>
public class PatientListDto
{
    /// <summary>شناسه بیمار</summary>
    public long Id { get; set; }

    /// <summary>شماره پرونده پزشکی</summary>
    public string MedicalRecordNo { get; set; } = string.Empty;

    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>کد ملی</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>جنسیت</summary>
    public Gender Gender { get; set; }

    /// <summary>موبایل</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>نام بیمه اصلی</summary>
    public string? InsuranceName { get; set; }

    /// <summary>شناسهٔ بیمهٔ اصلی پرونده — چرخهٔ ۶۷ (گزارش مالک):
    /// «در فرم پذیرش وقتی بیمار رو انتخاب میکنیم، فیلد نوع بیمه بر اساس بیمهٔ
    /// بیمار انتخاب نمیشه و هنوز آزاد رو نشون میده». ریشه: این DTO خروجیِ
    /// patients/search است و دسکتاپ برای نشاندن بیمهٔ بیمار روی کشوی «نوع بیمه»
    /// به InsuranceCompanyId نیاز دارد؛ فیلد نبود ⇒ همیشه تهی می‌رسید ⇒
    /// فرم پذیرش همیشه روی «آزاد» می‌ماند. نگاشت نام-به-نام (استراتژی Target)
    /// خود Patient.InsuranceCompanyId را می‌خواند — هیچ تغییر مپری لازم نیست.</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>سن محاسبه‌شده از تاریخ تولد — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// ستون «سن» در فهرست بیماران همیشه خالی بود چون DTO فهرست این فیلد را نداشت</summary>
    public int? Age { get; set; }

    /// <summary>تاریخ آخرین مراجعه شمسی</summary>
    public string? LastVisitDateShamsi { get; set; }

    /// <summary>برچسب‌های بیمار (CSV) — چرخهٔ ۶۹ (فاز ۳ / F-18): چیپ‌های رنگی در فهرست/پرونده</summary>
    public string? Tags { get; set; }

    /// <summary>بیمار ناشناس (اورژانسی) — پروندهٔ موقت بدون کد ملی</summary>
    public bool IsUnidentified { get; set; }

    /// <summary>کد موقت یکتای بیمار ناشناس — UNK-14050518-003</summary>
    public string? TemporaryCode { get; set; }
}

/// <summary>
/// جزئیات کامل بیمار برای صفحه پرونده
/// </summary>
public class PatientDetailDto
{
    /// <summary>شناسه بیمار</summary>
    public long Id { get; set; }

    /// <summary>شماره پرونده پزشکی</summary>
    public string MedicalRecordNo { get; set; } = string.Empty;

    /// <summary>کد ملی</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>نام پدر</summary>
    public string? FatherName { get; set; }

    /// <summary>جنسیت</summary>
    public Gender Gender { get; set; }

    /// <summary>وضعیت تأهل — نام فارسی: مجرد/متأهل/بیوه/مطلقه (چرخه بیست‌ویکم)</summary>
    public string? MaritalStatusFa { get; set; }

    /// <summary>تاریخ تولد شمسی</summary>
    public string? BirthDateShamsi { get; set; }

    /// <summary>سن محاسبه‌شده</summary>
    public int? Age { get; set; }

    /// <summary>گروه خونی</summary>
    public string? BloodType { get; set; }

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

    /// <summary>کد پستی — ۱۰ رقمی (چرخه بیست‌ویکم)</summary>
    public string? PostalCode { get; set; }

    /// <summary>شغل بیمار (چرخه بیست‌ویکم)</summary>
    public string? Occupation { get; set; }

    /// <summary>نام تماس اضطراری (چرخه بیست‌ویکم)</summary>
    public string? EmergencyContactName { get; set; }

    /// <summary>تلفن تماس اضطراری (چرخه بیست‌ویکم)</summary>
    public string? EmergencyContactPhone { get; set; }

    /// <summary>نسبت تماس اضطراری با بیمار (چرخه بیست‌ویکم)</summary>
    public string? EmergencyContactRelation { get; set; }

    /// <summary>بیمه اصلی — شناسه</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>نام بیمه اصلی</summary>
    public string? InsuranceName { get; set; }

    /// <summary>شماره دفترچه بیمه</summary>
    public string? InsuranceNumber { get; set; }

    /// <summary>نوع بیمه — اصلی/تبعی ۱/تبعی ۲ (چرخه بیست‌ویکم)</summary>
    public string? InsuranceType { get; set; }

    /// <summary>بیمه تکمیلی — شناسه</summary>
    public long? SupplementaryInsuranceId { get; set; }

    /// <summary>آلرژی‌ها</summary>
    public string? Allergies { get; set; }

    /// <summary>بیماری‌های مزمن</summary>
    public string? ChronicDiseases { get; set; }

    /// <summary>داروهای فعلی</summary>
    public string? CurrentMedications { get; set; }

    /// <summary>تاریخ ثبت‌نام شمسی</summary>
    public string? RegistrationDateShamsi { get; set; }

    /// <summary>برچسب‌های بیمار (CSV مثل «VIP، دیابت») — چرخهٔ ۶۹ (فاز ۳ / F-18)</summary>
    public string? Tags { get; set; }

    /// <summary>بیمار ناشناس (اورژانسی) — پروندهٔ موقت بدون کد ملی</summary>
    public bool IsUnidentified { get; set; }

    /// <summary>کد موقت یکتای بیمار ناشناس — UNK-14050518-003</summary>
    public string? TemporaryCode { get; set; }
}

/// <summary>درخواست ساخت پروندهٔ موقت بیمار ناشناس — فقط جنسیت و سن تقریبی اختیاری</summary>
public class CreateUnidentifiedPatientDto
{
    /// <summary>جنسیت: ۱=مرد، ۲=زن</summary>
    public byte Gender { get; set; } = 1;

    /// <summary>سن تقریبی (اختیاری) — برای برآورد تاریخ تولد</summary>
    public int? ApproximateAge { get; set; }
}

/// <summary>درخواست تکمیل هویت بیمار ناشناس با مشخصات واقعی</summary>
public class CompletePatientIdentityDto
{
    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>کد ملی واقعی — با رقم کنترلی اعتبارسنجی می‌شود</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>موبایل — الگوی 09XXXXXXXXX</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>جنسیت: ۱=مرد، ۲=زن</summary>
    public byte Gender { get; set; } = 1;
}

/// <summary>درخواست ادغام پروندهٔ موقت در پروندهٔ اصلی موجود</summary>
public class MergePatientsDto
{
    /// <summary>شناسهٔ پروندهٔ موقت (ناشناس) — منبع</summary>
    public long SourcePatientId { get; set; }

    /// <summary>شناسهٔ پروندهٔ اصلی موجود — مقصد</summary>
    public long TargetPatientId { get; set; }
}

/// <summary>نتیجهٔ تکمیل هویت — Completed یا Duplicate (پروندهٔ اصلی از قبل وجود دارد)</summary>
public class CompleteIdentityResultDto
{
    /// <summary>وضعیت: Completed / Duplicate</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>شناسهٔ بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>در وضعیت Duplicate: شناسهٔ پروندهٔ اصلی موجود با همان کد ملی</summary>
    public long? DuplicatePatientId { get; set; }

    /// <summary>در وضعیت Duplicate: نام پروندهٔ اصلی موجود</summary>
    public string? DuplicatePatientName { get; set; }
}
