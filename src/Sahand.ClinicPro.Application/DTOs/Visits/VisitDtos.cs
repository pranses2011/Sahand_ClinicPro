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
    decimal VisitFee);

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
    bool Complete = false);
