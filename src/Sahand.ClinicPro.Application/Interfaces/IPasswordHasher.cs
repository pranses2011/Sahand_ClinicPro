namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس هش رمز عبور با BCrypt — ضریب سختی ۱۲
/// </summary>
public interface IPasswordHasher
{
    /// <summary>هش کردن رمز عبور متن ساده</summary>
    string Hash(string password);

    /// <summary>بررسی تطابق رمز ورودی با هش ذخیره‌شده</summary>
    bool Verify(string password, string hash);

}
