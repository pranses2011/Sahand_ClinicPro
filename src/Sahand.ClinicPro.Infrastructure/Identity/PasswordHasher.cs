using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>هش رمز عبور با BCrypt-Net-Next — اعتبارسنجی سیاست پویا در PasswordPolicy</summary>
public class PasswordHasher : IPasswordHasher
{
    /// <summary>ضریب سختی BCrypt — تعادل امنیت و سرعت</summary>
    public const int WorkFactor = 12;

    /// <summary>هش کردن رمز عبور متن ساده</summary>
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    /// <summary>بررسی تطابق رمز ورودی با هش ذخیره‌شده</summary>
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
