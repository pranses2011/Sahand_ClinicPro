using AutoMapper;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Application.Common.Mappings;

/// <summary>
/// پروفایل نگاشت ماژول پزشکان
/// </summary>
public class DoctorProfile : Profile
{
    /// <summary>ثبت نقشه‌های پزشک</summary>
    public DoctorProfile()
    {
        CreateMap<Doctor, DoctorDetailDto>()
            .ForMember(d => d.SpecialtyName, o => o.Ignore());
    }
}
