using System;
using System.Collections.Generic;
using System.Linq;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// کلاس پایه همه موجودیت‌های دامنه
/// شامل کلید اصلی و مکانیزم رویدادهای دامنه است
/// </summary>
public abstract class BaseEntity
{
    /// <summary>کلید اصلی جداول (bigint IDENTITY)</summary>
    public long Id { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>رویدادهای دامنه که باید پس از ذخیره انتشار یابند</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>افزودن رویداد دامنه به صف انتشار</summary>
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>حذف رویداد دامنه از صف انتشار</summary>
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    /// <summary>پاک‌سازی صف رویدادهای دامنه</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
