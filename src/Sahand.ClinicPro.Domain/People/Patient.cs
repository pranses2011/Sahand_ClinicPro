using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// بیمار — مرکزی‌ترین موجودیت سیستم که تمام ماژول‌ها به آن وابسته هستند
/// </summary>
/// <remarks>
/// فیلدهای حساس مانند آدرس و بیماری‌ها با AES-256 رمزنگاری می‌شوند
/// کد ملی و موبایل دارای ایندکس با فیلتر IsDeleted=0 هستند
/// </remarks>
public class Patient : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره پرونده پزشکی - تولید خودکار با فرمت MRN-XXXXXX</summary>
    public string MedicalRecordNo { get; set; } = null!;

    /// <summary>کد ملی ۱۰ رقمی - یکتا و با الگوریتم رقم کنترلی اعتبارسنجی می‌شود</summary>
    public string NationalCode { get; set; } = null!;

    /// <summary>نام بیمار - حداکثر ۵۰ کاراکتر</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>نام خانوادگی بیمار - ایندکس‌شده برای جستجو</summary>
    public string LastName { get; set; } = null!;

    /// <summary>نام پدر بیمار</summary>
    public string? FatherName { get; set; }

    /// <summary>جنسیت بیمار</summary>
    public Gender Gender { get; set; }

    /// <summary>تاریخ تولد میلادی</summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>تاریخ تولد شمسی موازی - فرمت 1358/04/15</summary>
    public string? BirthDateShamsi { get; set; }

    /// <summary>گروه خونی مثل A+ یا O-</summary>
    public string? BloodType { get; set; }

    /// <summary>وضعیت تأهل</summary>
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>شماره موبایل ۱۱ رقمی - الگوی 09XXXXXXXXX - ایندکس‌شده</summary>
    public string Mobile { get; set; } = null!;

    /// <summary>تلفن ثابت</summary>
    public string? Phone { get; set; }

    /// <summary>ایمیل بیمار</summary>
    public string? Email { get; set; }

    /// <summary>آدرس - با AES-256 رمزنگاری می‌شود</summary>
    public string? Address { get; set; }

    /// <summary>شهر</summary>
    public string? City { get; set; }

    /// <summary>استان</summary>
    public string? Province { get; set; }

    /// <summary>کد پستی ۱۰ رقمی</summary>
    public string? PostalCode { get; set; }

    /// <summary>نام تماس اضطراری</summary>
    public string? EmergencyContactName { get; set; }

    /// <summary>تلفن تماس اضطراری</summary>
    public string? EmergencyContactPhone { get; set; }

    /// <summary>نسبت تماس اضطراری با بیمار</summary>
    public string? EmergencyContactRelation { get; set; }

    /// <summary>شناسه شرکت بیمه اصلی</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>شماره دفترچه بیمه</summary>
    public string? InsuranceNumber { get; set; }

    /// <summary>نوع بیمه - اصلی یا تبعی</summary>
    public string? InsuranceType { get; set; }

    /// <summary>شناسه بیمه تکمیلی</summary>
    public long? SupplementaryInsuranceId { get; set; }

    /// <summary>شناسه بیمار در سامانه سپاس</summary>
    public string? SepasPatientId { get; set; }

    /// <summary>شغل بیمار</summary>
    public string? Occupation { get; set; }

    /// <summary>سطح تحصیلات</summary>
    public EducationLevel? Education { get; set; }

    /// <summary>ملیت - پیش‌فرض ایرانی</summary>
    public string? Nationality { get; set; }

    /// <summary>نوع بیمار - عادی یا ویژه یا کارمند</summary>
    public PatientType PatientType { get; set; } = Domain.Enums.PatientType.Normal;

    /// <summary>آلرژی‌ها - آرایه JSON - رمزنگاری‌شده</summary>
    public string? Allergies { get; set; }

    /// <summary>بیماری‌های مزمن - JSON - رمزنگاری‌شده</summary>
    public string? ChronicDiseases { get; set; }

    /// <summary>داروهای فعلی بیمار - JSON</summary>
    public string? CurrentMedications { get; set; }

    /// <summary>سوابق خانوادگی - رمزنگاری‌شده</summary>
    public string? FamilyHistory { get; set; }

    /// <summary>مسیر عکس پروفایل</summary>
    public string? ProfilePicture { get; set; }

    /// <summary>داده‌های حساس رمزنگاری‌شده با AES-256</summary>
    public byte[]? EncryptedData { get; set; }

    /// <summary>تاریخ و ساعت ثبت‌نام</summary>
    public DateTime? RegistrationDate { get; set; }

    /// <summary>تاریخ آخرین مراجعه</summary>
    public DateTime? LastVisitDate { get; set; }

    /// <summary>فعال یا غیرفعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>شرکت بیمه اصلی بیمار</summary>
    public InsuranceCompany? InsuranceCompany { get; set; }

    /// <summary>شرکت بیمه تکمیلی</summary>
    public InsuranceCompany? SupplementaryInsurance { get; set; }

    /// <summary>نوبت‌های بیمار</summary>
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    /// <summary>پذیرش‌های بیمار</summary>
    public ICollection<Reception> Receptions { get; set; } = new List<Reception>();

    /// <summary>ویزیت‌های بیمار</summary>
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();

    /// <summary>نسخه‌های بیمار</summary>
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    /// <summary>فاکتورهای بیمار</summary>
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    /// <summary>سوابق پزشکی بیمار</summary>
    public ICollection<PatientHistory> Histories { get; set; } = new List<PatientHistory>();
}
