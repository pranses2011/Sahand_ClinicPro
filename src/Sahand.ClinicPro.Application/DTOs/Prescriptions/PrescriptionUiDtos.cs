namespace Sahand.ClinicPro.Application.DTOs.Prescriptions;

/// <summary>نتیجه جستجوی دارو — غنی طبق فرم ۱۲</summary>
public record MedicationSearchResultDto(
    long Id,
    string NameFa,
    string NameEn,
    string? Form,
    string? Strength,
    string? IrcCode,
    string? Manufacturer,
    string Country);

/// <summary>تداخل شناسایی‌شده — پنل ۴سطحی فرم ۱۲</summary>
public record InteractionDto(
    string Medication1Name,
    string Medication2Name,
    int Severity,
    string SeverityText,
    string SeverityColor,
    string? Mechanism,
    string? Recommendation,
    string? Description,
    bool IsAllergyConflict);

/// <summary>درخواست بررسی تداخل</summary>
public record CheckInteractionsRequest(
    long PatientId,
    List<long> MedicationIds);

/// <summary>نتیجه بررسی تداخل</summary>
public record CheckInteractionsResult(
    List<InteractionDto> Interactions,
    bool HasSevere,
    bool HasAllergyConflict);

/// <summary>تمپلیت نسخه پزشک</summary>
public record PrescriptionTemplateDto(
    long Id,
    string Name,
    string? Category,
    string TemplateJson);

/// <summary>درخواست ذخیره تمپلیت</summary>
public record SaveTemplateRequest(
    string Name,
    string? Category,
    string TemplateJson);
