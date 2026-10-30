namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>بررسی ابعاد اندازه‌گیری‌شده مرورگر؛ تضمین رفتار درایور یا صفحه‌بندی فیزیکی نیست.</summary>
public static class FixedCanvasPrintGuard
{
    /// <summary>وضعیت اندازه‌گیری کادرهای ثابت بوم.</summary>
    public enum Result { Clear, Overflow, Unavailable }

    /// <summary>ابعاد مفید و محتوای یک کادر ثابت.</summary>
    public readonly record struct Measurement(int ClientWidth, int ClientHeight, int ScrollWidth, int ScrollHeight);

    /// <summary>در نبود اندازه معتبر یا کادر قابل اندازه‌گیری نتیجه نامعلوم است، نه موفق.</summary>
    public static Result Evaluate(IEnumerable<Measurement> measurements)
    {
        var any = false;
        var overflow = false;
        foreach (var m in measurements)
        {
            any = true;
            if (m.ClientWidth <= 0 || m.ClientHeight <= 0 || m.ScrollWidth < 0 || m.ScrollHeight < 0) return Result.Unavailable;
            if (m.ScrollWidth > m.ClientWidth || m.ScrollHeight > m.ClientHeight) overflow = true;
        }
        return !any ? Result.Unavailable : overflow ? Result.Overflow : Result.Clear;
    }
}
