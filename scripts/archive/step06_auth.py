# -*- coding: utf-8 -*-
"""گام ۶ — ماژول Auth: DTOها + Command/Query های CQRS + Validatorها + Handlerها"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))


def write(rel, content):
    full = os.path.join(APP, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ DTOها ═══════════
write("DTOs/Auth/LoginRequestDto.cs", """
using System.ComponentModel.DataAnnotations;

namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// درخواست ورود به سامانه — با نام کاربری یا ایمیل
/// محدودیت نرخ: ۵ درخواست بر دقیقه و قفل ۳۰ دقیقه‌ای بعد از ۵ شکست
/// </summary>
public class LoginRequestDto
{
    /// <summary>نام کاربری یا ایمیل</summary>
    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [MaxLength(100, ErrorMessage = "نام کاربری حداکثر ۱۰۰ کاراکتر است")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>رمز عبور</summary>
    [Required(ErrorMessage = "رمز عبور الزامی است")]
    public string Password { get; set; } = string.Empty;
}
""")

write("DTOs/Auth/LoginResponseDto.cs", """
namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// پاسخ موفق ورود — توکن‌ها و اطلاعات کاربر
/// </summary>
public class LoginResponseDto
{
    /// <summary>توکن دسترسی JWT — با هر درخواست در سرآیند Authorization ارسال می‌شود</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>توکن تازه‌سازی — برای تمدید بدون ورود مجدد</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>عمر توکن دسترسی به ثانیه</summary>
    public int ExpiresIn { get; set; }

    /// <summary>نام کاربری</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>نام نمایشی کاربر</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>نقش‌های کاربر</summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>دسترسی‌های کاربر</summary>
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();

    /// <summary>آیا تغییر رمز عبور الزامی است</summary>
    public bool MustChangePassword { get; set; }
}
""")

write("DTOs/Auth/RefreshTokenRequestDto.cs", """
using System.ComponentModel.DataAnnotations;

namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// درخواست تمدید توکن با توکن تازه‌سازی
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>توکن تازه‌سازی دریافتی هنگام ورود</summary>
    [Required(ErrorMessage = "توکن تازه‌سازی الزامی است")]
    public string RefreshToken { get; set; } = string.Empty;
}
""")

write("DTOs/Auth/ChangePasswordDto.cs", """
using System.ComponentModel.DataAnnotations;

namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// درخواست تغییر رمز عبور توسط کاربر واردشده
/// </summary>
public class ChangePasswordRequestDto
{
    /// <summary>رمز عبور فعلی</summary>
    [Required(ErrorMessage = "رمز عبور فعلی الزامی است")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>رمز عبور جدید — حداقل ۸ کاراکتر با حرف و رقم</summary>
    [Required(ErrorMessage = "رمز عبور جدید الزامی است")]
    [MinLength(8, ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>تکرار رمز عبور جدید</summary>
    [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
    [Compare(nameof(NewPassword), ErrorMessage = "تکرار رمز عبور با رمز جدید یکسان نیست")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
""")

# ═══════════ Login ═══════════
write("Features/Auth/Commands/Login/LoginCommand.cs", """
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
""")

write("Features/Auth/Commands/Login/LoginCommandValidator.cs", """
using FluentValidation;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Login;

/// <summary>
/// اعتبارسنج فرمان ورود — با پیام‌های فارسی
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>قواعد اعتبارسنجی ورود</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("نام کاربری الزامی است")
            .MaximumLength(100).WithMessage("نام کاربری حداکثر ۱۰۰ کاراکتر است");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است")
            .MaximumLength(128).WithMessage("رمز عبور حداکثر ۱۲۸ کاراکتر است");
    }
}
""")

write("Features/Auth/Commands/Login/LoginCommandHandler.cs", """
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Login;

/// <summary>
/// گرداننده ورود — احراز هویت از IIdentityService و ساخت پاسخ توکن
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<LoginCommandHandler> _logger;

    /// <summary>سازنده با تزریق سرویس احراز هویت</summary>
    public LoginCommandHandler(IIdentityService identityService, ILogger<LoginCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>اجرای فرمان ورود</summary>
    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.AuthenticateAsync(
            request.UsernameOrEmail, request.Password, request.IpAddress, request.DeviceInfo,
            cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("ورود ناموفق برای {Username} از {Ip}",
                request.UsernameOrEmail, request.IpAddress ?? "نامشخص");
            return Result<LoginResponseDto>.Failure(
                new Error("LOGIN_FAILED", result.Error ?? "نام کاربری یا رمز عبور نادرست است"));
        }

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = result.ExpiresIn,
            Username = result.Username!,
            DisplayName = result.DisplayName!,
            Roles = result.Roles,
            Permissions = result.Permissions,
            MustChangePassword = result.MustChangePassword
        });
    }
}
""")

# ═══════════ Refresh ═══════════
write("Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs", """
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
""")

write("Features/Auth/Commands/RefreshToken/RefreshTokenCommandValidator.cs", """
using FluentValidation;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// اعتبارسنج فرمان تمدید توکن
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("توکن تازه‌سازی الزامی است");
    }
}
""")

write("Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs", """
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// گرداننده تمدید توکن — چرخش توکن تامه‌سازی و صدور جفت جدید
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    /// <summary>سازنده با تزریق سرویس احراز هویت</summary>
    public RefreshTokenCommandHandler(IIdentityService identityService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>اجرای فرمان تمدید</summary>
    public async Task<Result<LoginResponseDto>> Handle(RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.RefreshTokenAsync(
            request.RefreshToken, request.IpAddress, cancellationToken);

        if (!result.Succeeded)
            return Result<LoginResponseDto>.Failure(
                new Error("REFRESH_FAILED", result.Error ?? "توکن تازه‌سازی نامعتبر است"));

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = result.ExpiresIn
        });
    }
}
""")

# ═══════════ Logout ═══════════
write("Features/Auth/Commands/Logout/LogoutCommand.cs", """
using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Logout;

/// <summary>
/// فرمان خروج — ابطال توکن تامه‌سازی
/// </summary>
/// <param name="RefreshToken">توکن تازه‌سازی برای ابطال</param>
public record LogoutCommand(string RefreshToken) : IRequest<Result>;
""")

write("Features/Auth/Commands/Logout/LogoutCommandHandler.cs", """
using MediatR;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Logout;

/// <summary>
/// گرداننده خروج — ابطال توکن تامه‌سازی در دیتابیس
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;

    /// <summary>سازنده با تزریق سرویس احراز هویت</summary>
    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>اجرای فرمان خروج</summary>
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _identityService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
        return Result.Success();
    }
}
""")

# ═══════════ ChangePassword ═══════════
write("Features/Auth/Commands/ChangePassword/ChangePasswordCommand.cs", """
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
""")

write("Features/Auth/Commands/ChangePassword/ChangePasswordCommandValidator.cs", """
using FluentValidation;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// اعتبارسنج فرمان تغییر رمز عبور
/// </summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>قواعد اعتبارسنجی با پیام فارسی</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("رمز عبور فعلی الزامی است");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("رمز عبور جدید الزامی است")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد")
            .Matches("[a-zA-Z]").WithMessage("رمز عبور باید شامل حرف باشد")
            .Matches("\\d").WithMessage("رمز عبور باید شامل رقم باشد")
            .NotEqual(x => x.CurrentPassword).WithMessage("رمز جدید نباید با رمز فعلی یکسان باشد");
    }
}
""")

write("Features/Auth/Commands/ChangePassword/ChangePasswordCommandHandler.cs", """
using MediatR;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// گرداننده تغییر رمز عبور — با تاریخچه و سیاست امنیتی
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public ChangePasswordCommandHandler(IIdentityService identityService,
        ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    /// <summary>اجرای فرمان تغییر رمز</summary>
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? _currentUser.UserId
            ?? throw new Domain.Exceptions.NotFoundException("کاربر", "فعلی");

        return await _identityService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);
    }
}
""")

print("✅ گام ۶ — ماژول Auth کامل شد")
