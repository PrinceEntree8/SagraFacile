using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Interfaces;

public interface IOrderStateMachine
{
    OrderTransitionPolicy PolicyFor(Event @event);
}
