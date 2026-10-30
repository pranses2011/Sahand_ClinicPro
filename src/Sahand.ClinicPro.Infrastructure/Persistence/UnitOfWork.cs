using System.Collections.Concurrent;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Events;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence.Repositories;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// واحد کار — ذخیره تغییرات + انتشار رویدادهای دامنه از طریق MediatR
/// </summary>
public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ClinicProDbContext _context;
    private readonly IMediator _mediator;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    /// <summary>سازنده با تزریق کانتکست و میانجی</summary>
    public UnitOfWork(ClinicProDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    /// <summary>دریافت ریپازیتوری موجودیت — کش‌شده بر اساس نوع</summary>
    public IRepository<T> Repository<T>() where T : BaseEntity
        => (IRepository<T>)_repositories.GetOrAdd(
            typeof(T),
            _ => Activator.CreateInstance(
                typeof(Repository<>).MakeGenericType(typeof(T)),
                _context) ?? throw new InvalidOperationException("ساخت ریپازیتوری ناموفق بود"));

    /// <summary>ذخیره تغییرات و انتشار رویدادهای دامنه</summary>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(cancellationToken);
        return true;
    }

    /// <summary>انتشار رویدادهای دامنه معلق همه موجودیت‌های ردیابی‌شده</summary>
    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();
            foreach (var domainEvent in events)
            {
                // پوشش رویداد دامنه در نوتیفیکیشن MediatR با بازتاب نوع
                var notification = (INotification)Activator.CreateInstance(
                    typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                    domainEvent)!;
                await _mediator.Publish(notification, cancellationToken);
            }
        }
    }

    /// <summary>آزادسازی همگام برای محدوده‌های DI که با using بسته می‌شوند.</summary>
    public void Dispose() => _context.Dispose();

    /// <summary>آزادسازی ناهمگام منابع کانتکست.</summary>
    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
