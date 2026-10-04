using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// ویزیت - پرونده یک جلسه معاینه با الگوی SOAP
/// </summary>
/// <remarks>
/// الگوی SOAP استاندارد مستندسازی بالینی است
/// فیلدهای حساس با AES-256 رمزنگاری می‌شوند
/// </remarks>
public class Visit : AuditableEntity, ISoftDeletable
{
    /// <summary>کد ویزیت - فرمت VST-XXXXXXXX</summary>
    public string? VisitCode { get; set; }

    /// <summary>شناسه پذیرش مرتبط</summary>
    public long? ReceptionId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک</summary>
    public long DoctorId { get; set; }

    /// <summary>تاریخ و ساعت ویزیت</summary>
    public DateTime VisitDate { get; set; }

    /// <summary>تاریخ شمسی ویزیت</summary>
    public string? VisitDateShamsi { get; set; }

    /// <summary>نوع ویزیت</summary>
    public VisitType VisitType { get; set; } = Domain.Enums.VisitType.Initial;

    /// <summary>شکایت اصلی بیمار</summary>
    public string? ChiefComplaint { get; set; }

    /// <summary>شرح بیماری فعلی</summary>
    public string? HistoryOfPresentIllness { get; set; }

    /// <summary>سوابق پزشکی قبلی</summary>
    public string? PastMedicalHistory { get; set; }

    /// <summary>سوابق خانوادگی</summary>
    public string? FamilyHistory { get; set; }

    /// <summary>سوابق اجتماعی</summary>
    public string? SocialHistory { get; set; }

    /// <summary>مرور سیستم‌های بدن</summary>
    public string? ReviewOfSystems { get; set; }

    /// <summary>معاینه فیزیکی</summary>
    public string? PhysicalExamination { get; set; }

    /// <summary>بخش ذهنی الگوی SOAP</summary>
    public string? SoapSubjective { get; set; }

    /// <summary>بخش عینی الگوی SOAP</summary>
    public string? SoapObjective { get; set; }

    /// <summary>بخش ارزیابی الگوی SOAP - تشخیص</summary>
    public string? SoapAssessment { get; set; }

    /// <summary>بخش برنامه الگوی SOAP - درمان</summary>
    public string? SoapPlan { get; set; }

    /// <summary>شناسه تشخیص اصلی بر اساس ICD-10</summary>
    public long? PrimaryDiagnosisId { get; set; }

    /// <summary>تشخیص‌های ثانویه - آرایه JSON از کدهای ICD-10</summary>
    public string? SecondaryDiagnoses { get; set; }

    /// <summary>برنامه درمانی</summary>
    public string? TreatmentPlan { get; set; }

    /// <summary>تاریخ مراجعه بعدی</summary>
    public DateOnly? FollowUpDate { get; set; }

    /// <summary>دستورات پیگیری</summary>
    public string? FollowUpInstructions { get; set; }

    /// <summary>وضعیت ویزیت</summary>
    public VisitStatus Status { get; set; } = Domain.Enums.VisitStatus.Started;

    /// <summary>ساعت شروع ویزیت</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>ساعت پایان ویزیت</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>مدت ویزیت به دقیقه - محاسباتی</summary>
    public int? Duration { get; set; }

    /// <summary>هزینه ویزیت به ریال</summary>
    public decimal VisitFee { get; set; } = 0m;

    /// <summary>کد ارجاع سامانه سپاس</summary>
    public string? SepasReferenceCode { get; set; }

    /// <summary>پذیرش مرتبط</summary>
    public Reception? Reception { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>پزشک معالج</summary>
    public Doctor? Doctor { get; set; }

    /// <summary>تشخیص اصلی</summary>
    public Icd10Code? PrimaryDiagnosis { get; set; }

    /// <summary>علائم حیاتی این ویزیت</summary>
    public VitalSign? VitalSign { get; set; }

    /// <summary>نسخه‌های این ویزیت</summary>
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    /// <summary>درخواست‌های آزمایش</summary>
    public ICollection<LabOrder> LabOrders { get; set; } = new List<LabOrder>();

    /// <summary>درخواست‌های تصویربرداری</summary>
    public ICollection<ImagingOrder> ImagingOrders { get; set; } = new List<ImagingOrder>();
}
