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
    /// <summary>اندازه‌گیری DOM مرورگر داخلی؛ شکست دسترسی یا صفحه ناتمام مجوز چاپ نیست.</summary>
    public static Result InspectBrowserDocument(object? browserDocument)
    {
        try
        {
            dynamic? document = browserDocument;
            if (document is null || !string.Equals((string?)document.readyState, "complete", StringComparison.OrdinalIgnoreCase))
                return Result.Unavailable;
            dynamic elements = document.all;
            var count = Convert.ToInt32(elements.length);
            var measurements = new List<Measurement>();
            for (var i = 0; i < count; i++)
            {
                dynamic element = elements.item(i);
                if (!string.Equals((string?)element.tagName, "DIV", StringComparison.OrdinalIgnoreCase)) continue;
                dynamic style = element.style;
                if (!string.Equals((string?)style.position, "absolute", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals((string?)style.overflow, "hidden", StringComparison.OrdinalIgnoreCase)) continue;
                measurements.Add(new(Convert.ToInt32(element.clientWidth), Convert.ToInt32(element.clientHeight),
                    Convert.ToInt32(element.scrollWidth), Convert.ToInt32(element.scrollHeight)));
            }
            return Evaluate(measurements);
        }
        catch (Exception) { return Result.Unavailable; }
    }

}
