namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// پزشک - اطلاعات حرفه‌ای و قراردادی پزشکان
/// </summary>
public class Doctor : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پرسنل مرتبط در جدول کارمندان</summary>
    public long? EmployeeId { get; set; }

    /// <summary>شماره پروانه پزشکی - یکتا</summary>
    public string? MedicalLicenseNo { get; set; }

    /// <summary>کد نظام پزشکی - یکتا</summary>
    public string? NezamPezeshkiCode { get; set; }

    /// <summary>شناسه تخصص اصلی</summary>
    public long? SpecialtyId { get; set; }

    /// <summary>شناسه فوق‌تخصص - اختیاری</summary>
    public long? SubSpecialtyId { get; set; }

    /// <summary>مرتبه علمی - استاد یا دانشیار و غیره</summary>
    public string? AcademicRank { get; set; }

    /// <summary>بیوگرافی پزشک</summary>
    public string? Biography { get; set; }

    /// <summary>حق ویزیت به ریال</summary>
    public decimal ConsultationFee { get; set; } = 0m;

    /// <summary>مدت زمان هر ویزیت به دقیقه</summary>
    public int VisitDuration { get; set; } = 15;

    /// <summary>حداکثر بیمار در هر شیفت</summary>
    public int? MaxPatientsPerShift { get; set; }

    /// <summary>درصد سهم پزشک از ویزیت</summary>
    public decimal DoctorPercentage { get; set; } = 0m;

    /// <summary>تصویر امضای پزشک</summary>
    public byte[]? SignatureImage { get; set; }

    /// <summary>تصویر مهر پزشک</summary>
    public byte[]? StampImage { get; set; }

    /// <summary>نوبت‌دهی آنلاین فعال است</summary>
    public bool OnlineBookingEnabled { get; set; } = true;

    /// <summary>پذیرش بیمار جدید فعال است</summary>
    public bool AcceptsNewPatients { get; set; } = true;

    /// <summary>امتیاز پزشک از ۵</summary>
    public decimal Rating { get; set; } = 0m;

    /// <summary>فعال یا غیرفعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>رکورد پرسنلی مرتبط</summary>
    public Employee? Employee { get; set; }

    /// <summary>تخصص اصلی</summary>
    public Specialty? Specialty { get; set; }

    /// <summary>فوق‌تخصص</summary>
    public Specialty? SubSpecialty { get; set; }

    /// <summary>نوبت‌های پزشک</summary>
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    /// <summary>برنامه‌های کاری</summary>
    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();

    /// <summary>ویزیت‌های انجام‌شده</summary>
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();

    /// <summary>نسخه‌های صادرشده</summary>
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
