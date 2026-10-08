using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Prescriptions;

/// <summary>ردیف فهرست نسخه</summary>
public record PrescriptionListDto(
    long Id,
    string? PrescriptionCode,
    string PatientName,
    string DoctorName,
    string DateShamsi,
    PrescriptionType Type,
    PrescriptionStatus Status,
    bool IsElectronic);

/// <summary>قلم نسخه برای ثبت</summary>
public record PrescriptionItemDto(
    long MedicationId,
    int OrderIndex,
    string? Dosage,
    string? Frequency,
    string? Duration,
    string? Route,
    decimal Quantity,
    string? Unit,
    string? Instructions,
    bool IsSos = false);

/// <summary>درخواست ثبت نسخه</summary>
public record CreatePrescriptionDto(
    long PatientId,
    long DoctorId,
    long? VisitId,
    PrescriptionType Type = PrescriptionType.Medication,
    bool IsElectronic = false,
    InsuranceSystemType? InsuranceSystemType = null,
    bool IsNarcotic = false,
    string? Notes = null,
    List<PrescriptionItemDto> Items = null!);

/// <summary>قلم نسخه در پاسخ</summary>
public record PrescriptionItemDetailDto(
    long MedicationId,
    string MedicationName,
    int OrderIndex,
    string? Dosage,
    string? Frequency,
    string? Duration,
    decimal Quantity,
    string? Unit,
    string? Instructions);

/// <summary>
/// جزئیات کامل نسخه — چرخه بیست‌وسوم (گزارش شانزدهم مالک): فیلدهای PatientName و
/// DoctorName افزوده شدند تا پنجرهٔ جزئیات و «چاپ مجدد» نسخه، نام بیمار و پزشک را
/// داشته باشند (پیش‌تر فقط شناسه ارسال می‌شد و هدر پنجرهٔ جزئیات بدون نام می‌ماند).
/// </summary>
public record PrescriptionDetailDto(
    long Id,
    string? PrescriptionCode,
    long PatientId,
    string PatientName,
    long DoctorId,
    string DoctorName,
    long? VisitId,
    string DateShamsi,
    PrescriptionType Type,
    PrescriptionStatus Status,
    bool IsElectronic,
    bool IsNarcotic,
    string? Notes,
    List<PrescriptionItemDetailDto> Items);
