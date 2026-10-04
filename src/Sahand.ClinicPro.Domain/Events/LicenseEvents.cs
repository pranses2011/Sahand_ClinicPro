using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد انقضای لایسنس</summary>
public sealed record LicenseExpiredEvent(string LicenseKey) : DomainEventBase;

/// <summary>رویداد تمدید/فعال‌سازی موفق لایسنس</summary>
public sealed record LicenseActivatedEvent(string LicenseKey, bool IsOffline) : DomainEventBase;
