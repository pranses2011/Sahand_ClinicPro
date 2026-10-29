using System.Text.Json;

namespace Sahand.ClinicPro.Application.Features.Paraclinical;

/// <summary>بررسی اینکه ویرایشگر فعلی بتواند طرح دندان را بدون حذف فیلد یا مقدار بازسازی کند.</summary>
public static class DentalChartCompatibility
{
    private static bool UniqueKnown(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;
        var properties = element.EnumerateObject().ToList();
        return properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() == properties.Count &&
               properties.All(p => names.Contains(p.Name, StringComparer.Ordinal));
    }

    private static bool ToothNumber(string text)
    {
        if (!int.TryParse(text, out var n) || n.ToString() != text) return false;
        var quadrant = n / 10;
        var digit = n % 10;
        return quadrant is >= 1 and <= 4 ? digit is >= 1 and <= 8 :
               quadrant is >= 5 and <= 8 && digit is >= 1 and <= 5;
    }

    private static bool StringOrNull(JsonElement value) => value.ValueKind is JsonValueKind.String or JsonValueKind.Null;

    /// <summary>قالب ناشناخته، متن آزاد و ساختار ناقص قابل بازنویسی نیستند.</summary>
    public static bool CanEdit(string? json) => TryGetTotal(json, out _);

    /// <summary>محاسبه مجموع ریالی قابل ذخیره بدون گردکردن یا سرریز.</summary>
    public static bool TryGetTotal(string? json, out decimal total)
    {
        total = 0m;
        const decimal maxRial = 999_999_999_999_999_999m;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!UniqueKnown(root, "teeth", "rows", "extras") ||
                !root.TryGetProperty("teeth", out var teeth) || teeth.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array) return false;
            var toothProperties = teeth.EnumerateObject().ToList();
            if (toothProperties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != toothProperties.Count ||
                toothProperties.Any(p => !ToothNumber(p.Name) || p.Value.ValueKind != JsonValueKind.Number ||
                    !p.Value.TryGetInt32(out var condition) || condition is < 0 or > 6)) return false;
            foreach (var row in rows.EnumerateArray())
            {
                if (!UniqueKnown(row, "tooth", "procedure", "price", "status", "surface", "schedule") ||
                    !row.TryGetProperty("tooth", out var tooth) || tooth.ValueKind != JsonValueKind.Number ||
                    !tooth.TryGetInt32(out var number) || !ToothNumber(number.ToString()) ||
                    !row.TryGetProperty("price", out var price) || price.ValueKind != JsonValueKind.Number ||
                    !price.TryGetDecimal(out var amount) || amount < 0 || amount != decimal.Truncate(amount) ||
                    amount > maxRial - total) return false;
                total += amount;
                foreach (var name in new[] { "procedure", "status", "surface", "schedule" })
                    if (!row.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.String) return false;
            }
            if (!root.TryGetProperty("extras", out var extras)) return true;
            if (!UniqueKnown(extras, "perio", "medical", "prescription", "radio", "clinical", "consent", "isChildChart")) return false;
            foreach (var property in extras.EnumerateObject())
                if (property.Name is "consent" or "isChildChart")
                {
                    if (property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
                }
                else if (!StringOrNull(property.Value)) return false;
            return true;
        }
        catch (JsonException) { return false; }
    }
}
