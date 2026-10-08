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

/// <summary>نوع ریسک شناسایی‌شده — الگوریتم خطر تشخیصی چرخه ۱۳</summary>
public enum RiskKind
{
    /// <summary>تداخل دارو با دارو</summary>
    DrugInteraction = 1,

    /// <summary>تعارض با حساسیت دارویی بیمار</summary>
    Allergy = 2,

    /// <summary>خطر دوران بارداری (رده D/X)</summary>
    Pregnancy = 3,

    /// <summary>خطر شیردهی</summary>
    Lactation = 4,

    /// <summary>احتیاط سنی — کودک/سالمند</summary>
    AgeCaution = 5,

    /// <summary>احتیاط نارسایی کلیه/کبد</summary>
    OrganCaution = 6,

    /// <summary>هشدار دوز/عوارض (خواب‌آلودگی/وابستگی/حداکثر دوز)</summary>
    DoseCaution = 7,

    /// <summary>تعارض با تشخیص‌های زمینه‌ای بیمار</summary>
    DiseaseConflict = 8
}

/// <summary>تداخل/خطر شناسایی‌شده — پنل ۴سطحی فرم ۱۲ + ابعاد چرخه ۱۳</summary>
public record InteractionDto(
    string Medication1Name,
    string Medication2Name,
    int Severity,
    string SeverityText,
    string SeverityColor,
    string? Mechanism,
    string? Recommendation,
    string? Description,
    bool IsAllergyConflict,
    RiskKind Kind = RiskKind.DrugInteraction,
    string KindIcon = "💊",
    string KindLabel = "تداخل دارویی");

/// <summary>درخواست بررسی تداخل — نسخه چرخه ۱۳: اقلام با مقدار دوز اختیاری</summary>
public record CheckInteractionsItem(
    long MedicationId,
    decimal Quantity = 0,
    string? Frequency = null);

/// <summary>درخواست بررسی خطر تشخیصی کامل</summary>
public record CheckInteractionsRequest(
    long PatientId,
    List<long> MedicationIds,
    List<CheckInteractionsItem>? Items = null,
    bool IsPregnant = false,
    bool IsBreastfeeding = false);

/// <summary>نتیجه بررسی تداخل — چرخه ۱۳ با خلاصه ابعاد خطر</summary>
public record CheckInteractionsResult(
    List<InteractionDto> Interactions,
    bool HasSevere,
    bool HasAllergyConflict,
    bool HasPregnancyRisk = false,
    bool HasAbsoluteBlock = false,
    string? PatientAgeShamsi = null);

/// <summary>تمپلیت نسخه پزشک</summary>
public record PrescriptionTemplateDto(
    long Id,
    string Name,
    string? Category,
    string TemplateJson);

/// <summary>
/// درخواست ذخیره تمپلیت — چرخه بیست‌وسوم (گزارش شانزدهم مالک): فیلد DoctorId
/// اختیاری افزوده شد تا دسکتاپ هنگام ذخیرهٔ تمپلیت، پزشکِ انتخاب‌شدهٔ فرم نسخه را
/// هم بفرستد و تمپلیت به آن پزشک منتسب شود؛ اگر پزشکی انتخاب نشده باشد تمپلیت
/// به‌صورت عمومی (بدون پزشک) ذخیره می‌شود و دیگر با خطای کلید خارجی روبه‌رو
/// نمی‌شود.
/// </summary>
public record SaveTemplateRequest(
    string Name,
    string? Category,
    string TemplateJson,
    long? DoctorId = null);
