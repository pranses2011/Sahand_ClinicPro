using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// نشانگر رویداد دامنه — رویدادهای کسب‌وکار که پس از Commit منتشر می‌شوند
/// </summary>
public interface IDomainEvent
{
    /// <summary>زمان وقوع رویداد</summary>
    DateTime OccurredOn { get; }
}
