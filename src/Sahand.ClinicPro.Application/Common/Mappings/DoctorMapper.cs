using Riok.Mapperly.Abstractions;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Application.Common.Mappings;

/// <summary>
/// نگاشتگر ماژول پزشکان — تولیدشده در زمان کامپایل با Mapperly
/// </summary>
/// <remarks>جایگزین DoctorProfile خودکار — گام V11</remarks>
[Mapper]
public partial class DoctorMapper
{
    /// <summary>موجودیت به جزئیات — نام تخصص توسط گرداننده پر می‌شود</summary>
    [MapperIgnoreTarget(nameof(DoctorDetailDto.SpecialtyName))]
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial DoctorDetailDto ToDoctorDetailDto(Doctor doctor);
}