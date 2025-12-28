using EMR.Domain.Common;
using MediatR;

namespace EMR.Application.Common.Events;

/// <summary>
/// Wrapper to adapt domain events to MediatR INotification
/// This allows domain events to be published through MediatR without coupling the domain layer to MediatR
/// </summary>
public class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
