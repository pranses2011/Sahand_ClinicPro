using System.Security.Cryptography;
using System.Text;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>توکن تصادفی ۲۵۶ بیتی؛ فقط چکیده آن در جدول دعوت ثبت می‌شود.</summary>
internal static class SurveyPublicToken
{
    internal static string Create() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    internal static string? Hash(string? token)
    {
        if (token is null || token.Length != 43 || token.Any(c =>
            !(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))) return null;
        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
    }
}
