namespace Dotnet11.Unions.Api.Application;

public sealed class OrderService
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public OrderResult<Order> Create(Order order)
    {
        if (_orders.ContainsKey(order.Id))
            return new OrderResult<Order>(new Conflict($"Order '{order.Id}' already exists."));

        _orders[order.Id] = order;
        return new OrderResult<Order>(new Success<Order>(order));
    }

    public OrderResult<Order> Get(string id) => _orders.TryGetValue(id, out var order)
            ? new OrderResult<Order>(new Success<Order>(order))
            : new OrderResult<Order>(new NotFound());

    public OrderResult<Order> Confirm(string id) => Transition(id, orderState => orderState.Value is Pending,
            () => new Confirmed(DateTimeOffset.UtcNow),
            stateValue => $"Cannot confirm order in state '{Describe(stateValue)}'.");

    public OrderResult<Order> Ship(string id, string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return new OrderResult<Order>(new ValidationError(
                new[] { "Tracking number must not be empty." }));

        return Transition(id, orderState => orderState.Value is Confirmed,
            () => new Shipped(DateTimeOffset.UtcNow, trackingNumber),
            stateValue => $"Cannot ship order in state '{Describe(stateValue)}'.");
    }

    public OrderResult<Order> Deliver(string id) => Transition(id, orderState => orderState.Value is Shipped,
            () => new Delivered(DateTimeOffset.UtcNow),
            stateValue => $"Cannot deliver order in state '{Describe(stateValue)}'.");

    public OrderResult<Order> Cancel(string id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return new OrderResult<Order>(new ValidationError(
                new[] { "Cancellation reason must not be empty." }));

        return Transition(id, orderState => orderState.Value is Pending or Confirmed or Shipped,
            () => new Cancelled(DateTimeOffset.UtcNow, reason),
            stateValue => $"Cannot cancel order in state '{Describe(stateValue)}'.");
    }

    public OrderResult<Order>[] GetAll() => [.. _orders.Values
            .Select(o => new OrderResult<Order>(new Success<Order>(o)))];

    private OrderResult<Order> Transition(
        string id,
        Func<OrderState, bool> canApply,
        Func<OrderState> createNext,
        Func<object, string> invalidMessage)
    {
        if (!_orders.TryGetValue(id, out var order))
            return new OrderResult<Order>(new NotFound());

        if (canApply(order.State))
        {
            var nextState = createNext();
            var updated = order with { State = nextState };
            _orders[id] = updated;
            return new OrderResult<Order>(new Success<Order>(updated));
        }

        return new OrderResult<Order>(new ValidationError(
            new[] { invalidMessage(order.State.Value) }));
    }

    private static string Describe(object stateValue) => stateValue switch
    {
        Pending _ => "pending",
        Confirmed _ => "confirmed",
        Shipped _ => "shipped",
        Delivered _ => "delivered",
        Cancelled _ => "cancelled",
        _ => stateValue.GetType().Name.ToLowerInvariant()
    };
}
