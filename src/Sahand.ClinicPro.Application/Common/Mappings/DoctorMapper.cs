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
    /// <summary>موجودیت به جزئیات — نام تخصص و تصاویر Base64 توسط گرداننده پر می‌شوند</summary>
    [MapperIgnoreTarget(nameof(DoctorDetailDto.SpecialtyName))]
    [MapperIgnoreTarget(nameof(DoctorDetailDto.SignatureImageBase64))]
    [MapperIgnoreTarget(nameof(DoctorDetailDto.StampImageBase64))]
    [MapperIgnoreTarget(nameof(DoctorDetailDto.HasPhoto))] // چرخه هجدهم — دستی از Employee.ProfilePicture
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial DoctorDetailDto ToDoctorDetailDto(Doctor doctor);

    /// <summary>تبدیل بایت تصویر به رشته Base64 — برای امضا و مهر</summary>
    private static string? ToBase64String(byte[]? bytes)
        => bytes is null || bytes.Length == 0 ? null : Convert.ToBase64String(bytes);
}