namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// نتیجه تمدید توکن با توکن تازه‌سازی
/// </summary>
public sealed record RefreshResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string? Error)
{
    /// <summary>ساخت نتیجه موفق</summary>
    public static RefreshResult Success(string accessToken, string refreshToken, int expiresIn) =>
        new(true, accessToken, refreshToken, expiresIn, null);

    /// <summary>ساخت نتیجه ناموفق</summary>
    public static RefreshResult Failure(string error) =>
        new(false, null, null, 0, error);
}
