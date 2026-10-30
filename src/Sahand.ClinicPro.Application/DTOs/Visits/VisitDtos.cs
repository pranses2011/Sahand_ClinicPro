using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Visits;

/// <summary>ردیف فهرست ویزیت‌ها</summary>
public record VisitListDto(
    long Id,
    string? VisitCode,
    string PatientName,
    string DoctorName,
    string DateShamsi,
    VisitStatus Status,
    string? ChiefComplaint);

/// <summary>جزئیات کامل ویزیت با الگوی SOAP</summary>
public record VisitDetailDto(
    long Id,
    string? VisitCode,
    long PatientId,
    long DoctorId,
    string DateShamsi,
    VisitType Type,
    VisitStatus Status,
    string? ChiefComplaint,
    string? HistoryOfPresentIllness,
    string? PastMedicalHistory,
    string? PhysicalExamination,
    string? SoapSubjective,
    string? SoapObjective,
    string? SoapAssessment,
    string? SoapPlan,
    long? PrimaryDiagnosisId,
    string? PrimaryDiagnosisCode,
    string? TreatmentPlan,
    string? FollowUpDateShamsi,
    decimal VisitFee,
    string? FamilyHistory = null,
    string? SocialHistory = null,
    string? ReviewOfSystems = null,
    string? SecondaryDiagnoses = null,
    string? FollowUpInstructions = null,
    PatientContextDto? Patient = null,
    VitalSignsViewDto? VitalSigns = null,
    // ═══ چرخهٔ ۷۸ — عنوان تشخیص اصلی ═══ کلاینت برای بازگرداندن شناسهٔ واقعی
    // هنگام ویرایش ویزیت به آن نیاز دارد؛ پیش‌تر فقط Code می‌رفت و کلاینت
    // جای‌نگهدارِ «Id=0» می‌ساخت که در پایان ویزیت به تهی ترجمه می‌شد
    string? PrimaryDiagnosisTitle = null,
    long? ReceptionId = null,
    string? EditVersion = null);

/// <summary>درخواست شروع یا ذخیره ویزیت</summary>
public record SaveVisitDto(
    long? Id,
    long PatientId,
    long DoctorId,
    long? ReceptionId,
    VisitType Type = VisitType.Initial,
    string? ChiefComplaint = null,
    string? HistoryOfPresentIllness = null,
    string? PastMedicalHistory = null,
    string? PhysicalExamination = null,
    string? SoapSubjective = null,
    string? SoapObjective = null,
    string? SoapAssessment = null,
    string? SoapPlan = null,
    long? PrimaryDiagnosisId = null,
    string? TreatmentPlan = null,
    string? FollowUpDateShamsi = null,
    bool Complete = false,
    string? FamilyHistory = null,
    string? SocialHistory = null,
    string? ReviewOfSystems = null,
    string? SecondaryDiagnoses = null,
    string? FollowUpInstructions = null,
    VitalSignsInputDto? VitalSigns = null,
    string? EditVersion = null);

/// <summary>ورودی علائم حیاتی در ذخیره ویزیت — گام U6 تب ۲ فرم ۱۱</summary>
public record VitalSignsInputDto(
    int? BloodPressureSystolic = null,
    int? BloodPressureDiastolic = null,
    int? HeartRate = null,
    decimal? Temperature = null,
    int? OxygenSaturation = null,
    decimal? Weight = null,
    decimal? Height = null,
    int? BloodSugar = null);

/// <summary>اطلاعات زمینه بیمار در فرم ویزیت — سایدبار فرم ۱۱</summary>
public record PatientContextDto(
    string FullName,
    string MedicalRecordNo,
    string NationalCode,
    int? Age,
    string GenderText,
    string? BloodType,
    string Mobile,
    string? InsuranceName,
    string? Allergies,
    string? ChronicDiseases,
    string? CurrentMedications);

/// <summary>علائم حیاتی نمایشی</summary>
public record VitalSignsViewDto(
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? HeartRate,
    decimal? Temperature,
    int? OxygenSaturation,
    decimal? Weight,
    decimal? Height,
    decimal? Bmi,
    int? BloodSugar);
