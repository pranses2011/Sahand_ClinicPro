using AutoMapper;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Application.Common.Mappings;

/// <summary>
/// پروفایل نگاشت ماژول بیماران
/// </summary>
public class PatientProfile : Profile
{
    /// <summary>ثبت نقشه‌های بیمار</summary>
    public PatientProfile()
    {
        // موجودیت به فهرست
        CreateMap<Patient, PatientListDto>()
            .ForMember(d => d.InsuranceName, o => o.MapFrom(s => s.InsuranceCompany != null ? s.InsuranceCompany.Name : null))
            .ForMember(d => d.LastVisitDateShamsi, o => o.Ignore());

        // موجودیت به جزئیات
        CreateMap<Patient, PatientDetailDto>()
            .ForMember(d => d.Age, o => o.Ignore())
            .ForMember(d => d.InsuranceName, o => o.Ignore())
            .ForMember(d => d.RegistrationDateShamsi, o => o.Ignore());

        // DTO ورودی به موجودیت — پرونده و حسابرسی خودکار است
        CreateMap<CreatePatientDto, Patient>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.MedicalRecordNo, o => o.Ignore())
            .ForMember(d => d.MedicalRecordNo, o => o.Ignore());
    }
}
