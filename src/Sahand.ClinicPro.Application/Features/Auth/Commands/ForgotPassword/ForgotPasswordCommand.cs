using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// فرمان «فراموشی رمز» — چرخهٔ ۸۲ (گزارش مالک):
/// «در صفحهٔ ورود، فراموشی رمز کار نمی‌کند؛ رمز به ایمیل کاربر ارسال شود».
/// </summary>
/// <param name="UsernameOrEmail">نام کاربری یا ایمیل ثبت‌شدهٔ حساب</param>
public record ForgotPasswordCommand(string UsernameOrEmail) : IRequest<Result>;
