using Sahand.ClinicPro.Application.DTOs.Visits;

namespace Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit;

/// <summary>مرز ذخیره علائم حیاتی؛ جایگزین قضاوت بالینی نیست.</summary>
public static class VisitVitalsSafety
{
    public static bool IsValid(VitalSignsInputDto? v)
    {
        if (v is null) return true;
        if (v.BloodPressureSystolic is < 0 || v.BloodPressureDiastolic is < 0 ||
            v.HeartRate is < 0 || v.BloodSugar is < 0 || v.OxygenSaturation is < 0 or > 100)
            return false;
        if (!Fits(v.Temperature, 999.9m, 1) || !Fits(v.Weight, 999.99m, 2) || !Fits(v.Height, 999.99m, 2))
            return false;
        if (v.Weight is > 0 && v.Height is > 0)
        {
            var metres = v.Height.Value / 100m;
            if (Math.Round(v.Weight.Value / metres / metres, 1) > 99.99m) return false;
        }
        return true;
    }

    private static bool Fits(decimal? value, decimal max, int scale) =>
        value is null || (value >= 0 && value <= max && decimal.Round(value.Value, scale) == value);
}
