using Riok.Mapperly.Abstractions;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Application.Common.Mappings;

/// <summary>
/// نگاشتگر ماژول بیماران — تولیدشده در زمان کامپایل با Mapperly
/// </summary>
/// <remarks>
/// جایگزین PatientProfile خودکار (گام ۷) — گام V11:
/// حذف AutoMapper 13.0.1 به‌دلیل هشدار امنیتی GHSA-rvv3-g6hj-g44x (DoS نگاشت گراف عمیق).
/// استراتژی الزام: فقط اعضای هدف نگاشت شوند (Source آزاد است).
/// </remarks>
[Mapper]
public partial class PatientMapper
{
    /// <summary>موجودیت به ردیف فهرست — نام بیمه از مسیر ناوبری</summary>
    [MapProperty("InsuranceCompany.Name", nameof(PatientListDto.InsuranceName))]
    [MapperIgnoreTarget(nameof(PatientListDto.LastVisitDateShamsi))]
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial PatientListDto ToPatientListDto(Patient patient);

    /// <summary>نگاشت فهرستی</summary>
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial List<PatientListDto> ToPatientListDtos(List<Patient> patients);

    /// <summary>موجودیت به جزئیات — فیلدهای محاسباتی توسط گرداننده پر می‌شوند
    /// چرخه بیست‌ویکم: MaritalStatusFa در گرداننده از enum ترجمه می‌شود</summary>
    [MapperIgnoreTarget(nameof(PatientDetailDto.Age))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.InsuranceName))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.RegistrationDateShamsi))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.MaritalStatusFa))]
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial PatientDetailDto ToPatientDetailDto(Patient patient);

    /// <summary>DTO ورودی به موجودیت — پرونده و حسابرسی خودکار است</summary>
    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.MedicalRecordNo))]
    [MapperIgnoreTarget(nameof(Patient.BirthDate))]
    [MapperIgnoreTarget(nameof(Patient.MaritalStatus))]
    [MapperIgnoreTarget(nameof(Patient.Education))]
    [MapperIgnoreTarget(nameof(Patient.PatientType))]
    [MapperIgnoreTarget(nameof(Patient.RegistrationDate))]
    [MapperIgnoreTarget(nameof(Patient.LastVisitDate))]
    [MapperIgnoreTarget(nameof(Patient.InsuranceCompany))]
    [MapperIgnoreTarget(nameof(Patient.SupplementaryInsurance))]
    [MapperIgnoreTarget(nameof(Patient.Appointments))]
    [MapperIgnoreTarget(nameof(Patient.Receptions))]
    [MapperIgnoreTarget(nameof(Patient.Visits))]
    [MapperIgnoreTarget(nameof(Patient.Prescriptions))]
    [MapperIgnoreTarget(nameof(Patient.Invoices))]
    [MapperIgnoreTarget(nameof(Patient.Histories))]
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial Patient ToPatient(CreatePatientDto dto);

    /// <summary>
    /// پرکردن فیلدهای محاسباتی ردیف‌های فهرست بیماران — چرخه بیست‌وسوم (گزارش شانزدهم مالک).
    /// ستون‌های «سن» و «آخرین مراجعه» در فهرست بیماران همیشه خالی بودند چون:
    /// ۱) نگاشتگر Mapperly سن را محاسبه نمی‌کند (فیلد هدف از موجودیت مستقیم نمی‌آید)
    /// ۲) LastVisitDateShamsi عمداً از نگاشت حذف شده بود و هیچ گرداننده‌ای پرش نمی‌کرد
    /// این متد پس از نگاشت در هر دو گردانندهٔ «فهرست بیماران» و «جستجوی سریع» فراخوانی
    /// می‌شود تا هر دو نقطهٔ نمایش فهرست، مقادیر تازه و درست داشته باشند (قانون مشابه‌یابی).
    /// </summary>
    /// <param name="dtos">ردیف‌های نگاشت‌شده — به همان ترتیب موجودیت‌ها</param>
    /// <param name="patients">موجودیت‌های منبع — ترتیب حفظ می‌شود</param>
    public static void EnrichListComputedFields(List<PatientListDto> dtos, IReadOnlyList<Patient> patients)
    {
        // پیمایش جفتی (dto ↔ موجودیت)؛ شرط i < هر دو برای اطمینان از هم‌طولی
        for (var i = 0; i < dtos.Count && i < patients.Count; i++)
        {
            // روخش محلی تاریخ تولد — رفع CS8629: تحلیل‌گر تهی‌بودن را از طریق
            // اندیس‌گذاری دوبار نمی‌فهمد؛ متغیر محلی مطمئن است
            var birth = patients[i].BirthDate;

            // سن از تاریخ تولد میلادی با تقویم شمسی (سال‌گذرانی درست)
            dtos[i].Age = birth.HasValue
                ? PersianDateHelper.CalculateAge(birth.Value.ToDateTime(TimeOnly.MinValue))
                : null;

            // آخرین مراجعه → شمسی با ارقام فارسی (سازگار با زبان تمام‌فارسی رابط کاربری)
            var lastVisit = patients[i].LastVisitDate;
            dtos[i].LastVisitDateShamsi = lastVisit.HasValue
                ? PersianDateHelper.ToShamsiFa(lastVisit.Value)
                : null;
        }
    }
}