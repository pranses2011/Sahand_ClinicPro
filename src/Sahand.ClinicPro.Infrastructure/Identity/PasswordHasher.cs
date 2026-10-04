using System.Text.RegularExpressions;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>
/// هش رمز عبور با BCrypt-Net-Next — ضریب سختی ۱۲
/// </summary>
public partial class PasswordHasher : IPasswordHasher
{
    /// <summary>ضریب سختی BCrypt — تعادل امنیت و سرعت</summary>
    public const int WorkFactor = 12;

    [GeneratedRegex(@"^(?=.*[a-zA-Z])(?=.*\d).{8,}$")]
    private static partial Regex PolicyRegex();

    /// <summary>هش کردن رمز عبور متن ساده</summary>
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    /// <summary>بررسی تطابق رمز ورودی با هش ذخیره‌شده</summary>
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    /// <summary>سیاست رمز: حداقل ۸ کاراکتر شامل حرف و رقم</summary>
    public bool MeetsPolicy(string password, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد";
            return false;
        }
        if (!PolicyRegex().IsMatch(password))
        {
            errorMessage = "رمز عبور باید شامل حرف و رقم باشد";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }
}
