using MediatR;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// فرمان تمدید توکن دسترسی با توکن تامه‌سازی — همراه چرخش توکن
/// </summary>
/// <param name="RefreshToken">توکن تامه‌سازی معتبر</param>
/// <param name="IpAddress">نشانی IP درخواست‌کننده</param>
public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<Result<LoginResponseDto>>;
