using MediatR;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Login;

/// <summary>
/// فرمان ورود به سامانه — احراز هویت و صدور توکن‌ها
/// </summary>
/// <param name="UsernameOrEmail">نام کاربری یا ایمیل</param>
/// <param name="Password">رمز عبور</param>
/// <param name="IpAddress">نشانی IP درخواست‌کننده</param>
/// <param name="DeviceInfo">اطلاعات دستگاه (UserAgent کلاینت WPF)</param>
public record LoginCommand(
    string UsernameOrEmail,
    string Password,
    string? IpAddress = null,
    string? DeviceInfo = null) : IRequest<Result<LoginResponseDto>>;
