using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// کلاس پایه رویدادهای دامنه
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <summary>زمان وقوع رویداد (UTC)</summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
