using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Common.Events;

/// <summary>
/// پوشش رویداد دامنه برای انتشار از طریق MediatR — لایه دامنه به MediatR وابسته نیست
/// </summary>
/// <typeparam name="TDomainEvent">نوع رویداد دامنه</typeparam>
/// <param name="DomainEvent">رویداد دامنه اصلی</param>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : INotification where TDomainEvent : IDomainEvent;
