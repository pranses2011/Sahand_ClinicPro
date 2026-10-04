using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد هشدار کمبود موجودی دارو — برای نوتیفیکیشن و پیامک انباردار</summary>
public sealed record LowStockAlertEvent(long InventoryId, long MedicationId, string MedicationName, decimal Quantity, decimal ReorderLevel) : DomainEventBase;

/// <summary>رویداد نزدیک بودن تاریخ انقضای دارو</summary>
public sealed record MedicationExpiringEvent(long InventoryId, string MedicationName, int DaysRemaining) : DomainEventBase;
