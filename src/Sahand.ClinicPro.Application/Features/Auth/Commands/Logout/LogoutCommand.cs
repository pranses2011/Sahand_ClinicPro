using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Logout;

/// <summary>
/// فرمان خروج — ابطال توکن تامه‌سازی
/// </summary>
/// <param name="RefreshToken">توکن تازه‌سازی برای ابطال</param>
public record LogoutCommand(string RefreshToken) : IRequest<Result>;
