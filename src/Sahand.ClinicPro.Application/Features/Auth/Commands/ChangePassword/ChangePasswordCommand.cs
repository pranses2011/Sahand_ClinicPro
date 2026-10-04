using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// فرمان تغییر رمز عبور کاربر واردشده فعلی
/// </summary>
/// <param name="CurrentPassword">رمز عبور فعلی</param>
/// <param name="NewPassword">رمز عبور جدید</param>
/// <param name="UserId">شناسه کاربر فعلی از ICurrentUserService پر می‌شود</param>
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    long? UserId = null) : IRequest<Result>;
