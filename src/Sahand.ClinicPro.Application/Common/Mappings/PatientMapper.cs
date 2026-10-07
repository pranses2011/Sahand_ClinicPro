using Riok.Mapperly.Abstractions;
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
}