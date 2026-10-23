namespace Sahand.ClinicPro.Application.Common.Security;

/// <summary>سیاست رمز عبور مشترک برای ساخت کاربر و تغییر رمز.</summary>
public sealed record PasswordPolicy(
    int MinPasswordLength = 8,
    bool RequireMixedCase = true,
    bool RequireDigit = true,
    bool RequireSpecialChar = true)
{
    public static PasswordPolicy FromSettings(IEnumerable<KeyValuePair<string, string?>> settings)
    {
        var values = settings.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        return new PasswordPolicy(
            ReadInt(values, "MinPasswordLength", 8),
            ReadBool(values, "RequireMixedCase", true),
            ReadBool(values, "RequireDigit", true),
            ReadBool(values, "RequireSpecialChar", true));
    }

    public string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password)) return "رمز عبور الزامی است";
        if (password.Length < Math.Max(1, MinPasswordLength))
            return $"رمز عبور باید حداقل {Math.Max(1, MinPasswordLength)} نویسه باشد";
        if (RequireMixedCase && (!password.Any(char.IsUpper) || !password.Any(char.IsLower)))
            return "رمز عبور باید شامل حروف کوچک و بزرگ باشد";
        if (RequireDigit && !password.Any(char.IsDigit))
            return "رمز عبور باید دست‌کم یک عدد داشته باشد";
        if (RequireSpecialChar && !password.Any(ch => !char.IsLetterOrDigit(ch)))
            return "رمز عبور باید دست‌کم یک نویسهٔ ویژه داشته باشد";
        return null;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string?> values, string key, int fallback)
        => values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? Math.Clamp(parsed, 1, 128)
            : fallback;

    private static bool ReadBool(IReadOnlyDictionary<string, string?> values, string key, bool fallback)
        => values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;
}

/// <summary>منبع اجرای سیاست رمز از تنظیمات پایگاه‌داده.</summary>
public interface IPasswordPolicyProvider
{
    Task<PasswordPolicy> GetCurrentAsync(CancellationToken cancellationToken = default);
}

/// <summary>تنظیمات محاسبه بیمه، مستقل برای ویزیت و خدمت درمانی.</summary>
public sealed record InsuranceBillingPolicy(bool ApplyToDoctorVisits = true, bool ApplyToMedicalServices = true)
{
    public static InsuranceBillingPolicy FromSettings(IEnumerable<KeyValuePair<string, string?>> settings)
    {
        var values = settings.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        return new InsuranceBillingPolicy(
            ReadBool(values, "ApplyToDoctorVisits", true),
            ReadBool(values, "ApplyToMedicalServices", true));
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string?> values, string key, bool fallback)
        => values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;
}
