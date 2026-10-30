namespace Sahand.ClinicPro.Application.Features.Paraclinical;

/// <summary>اعتبار نمایش و ظرفیت ذخیره مقادیر معاینه چشم؛ جایگزین تصمیم بالینی نیست.</summary>
public static class EyeExamValueSafety
{
    private static bool Lens(decimal? value) => value is null ||
        (value >= -99.99m && value <= 99.99m && decimal.Round(value.Value, 2) == value.Value);
    private static bool Axis(int? value) => value is null or >= 0 and <= 180;
    private static bool Iop(int? value) => value is null or >= 0;
    private static bool PupilDistance(decimal? value) => value is null ||
        (value > 0 && value <= 999.9m && decimal.Round(value.Value, 1) == value.Value);

    /// <summary>رد ورودی ناممکن یا مقداری که دیتابیس آن را گرد یا حذف می‌کند.</summary>
    public static bool IsValid(decimal? rightSphere, decimal? rightCylinder, int? rightAxis,
        decimal? leftSphere, decimal? leftCylinder, int? leftAxis,
        int? rightIop, int? leftIop, decimal? pupilDistance) =>
        Lens(rightSphere) && Lens(rightCylinder) && Axis(rightAxis) &&
        Lens(leftSphere) && Lens(leftCylinder) && Axis(leftAxis) &&
        Iop(rightIop) && Iop(leftIop) && PupilDistance(pupilDistance);
}
