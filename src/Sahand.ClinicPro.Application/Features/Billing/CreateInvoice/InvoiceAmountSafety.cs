using Sahand.ClinicPro.Application.DTOs.Billing;

namespace Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;

/// <summary>محدوده و دقت قابل ذخیره در ستون‌های فاکتور، پیش از هر عمل مالی.</summary>
public static class InvoiceAmountSafety
{
    public const decimal MaxRial = 999999999999999999m;
    public static bool ValidPercent(decimal value) => value is >= 0m and <= 100m && decimal.Round(value, 2) == value;
    public static bool Valid(CreateInvoiceDto dto)
    {
        if (dto.Items is null || dto.Items.Count is < 1 or > 500 || !Enum.IsDefined(dto.Type) || !ValidPercent(dto.TaxPercent)) return false;
        decimal grossSum = 0;
        foreach (var item in dto.Items)
        {
            if (item is null || item.Quantity is <= 0 or > 99999999.99m || decimal.Round(item.Quantity, 2) != item.Quantity ||
                item.UnitPrice is < 0 or > MaxRial || decimal.Truncate(item.UnitPrice) != item.UnitPrice ||
                !ValidPercent(item.DiscountPercent) || !ValidPercent(item.DoctorSharePercent) ||
                item.ServiceId is <= 0 || item.DoctorId is <= 0 || item.MedicationId is <= 0 || item.PerformedById is <= 0 ||
                (item.MedicationId.HasValue && (item.ServiceId.HasValue || item.DoctorId.HasValue))) return false;
            if (item.UnitPrice > 0 && item.Quantity > MaxRial / item.UnitPrice) return false;
            grossSum += item.Quantity * item.UnitPrice;
            if (grossSum > MaxRial) return false;
        }
        return true;
    }
}
