namespace Dotnet11.Unions.Api;

public static class OrderEndpoints
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders")
            .WithTags("Orders")
            .WithName("Orders");


        orders.MapGet("/", (OrderService service) =>
        {
            var all = service.GetAll();
            return Results.Ok(all);
        })
        .WithName("GetAllOrders")
        .Produces<Order>(StatusCodes.Status200OK);


        orders.MapGet("/{id}", (string id, OrderService service) =>
        {
            var result = service.Get(id);
            return result.Value switch
            {
                Success<Order> success => Results.Ok(success.Value),
                NotFound => Results.NotFound(),
                ValidationError ve => Results.BadRequest(new { errors = ve.Errors }),
                Conflict c => Results.Conflict(new { message = c.Message }),
                _ => Results.Problem("Unhandled order result case.")
            };
        })
        .WithName("GetOrderById")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);


        orders.MapPost("/", (Order order, OrderService service) =>
        {
            var result = service.Create(order);

            return result.Value switch
            {
                Success<Order> s => Results.CreatedAtRoute(
                    "GetOrderById", new { id = s.Value.Id }, s.Value),
                Conflict c => Results.Conflict(new { message = c.Message }),
                _ => Results.Problem("Unexpected result.")
            };
        })
        .WithName("CreateOrder")
        .Produces<Order>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status409Conflict);


        orders.MapPost("/{id}/confirm", (string id, OrderService service) =>
        {
            var result = service.Confirm(id);

            return result.Value switch
            {
                Success<Order> s => Results.Ok(s.Value),
                NotFound => Results.NotFound(),
                ValidationError ve => Results.BadRequest(new { errors = ve.Errors }),
                _ => Results.Problem("Unexpected result.")
            };
        })
        .WithName("ConfirmOrder")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);


        orders.MapPost("/{id}/ship", (string id, string trackingNumber, OrderService service) =>
        {
            var result = service.Ship(id, trackingNumber);

            return result.Value switch
            {
                Success<Order> s => Results.Ok(s.Value),
                NotFound => Results.NotFound(),
                ValidationError ve => Results.BadRequest(new { errors = ve.Errors }),
                _ => Results.Problem("Unexpected result.")
            };
        })
        .WithName("ShipOrder")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);


        orders.MapPost("/{id}/deliver", (string id, OrderService service) =>
        {
            var result = service.Deliver(id);

            return result.Value switch
            {
                Success<Order> s => Results.Ok(s.Value),
                NotFound => Results.NotFound(),
                ValidationError ve => Results.BadRequest(new { errors = ve.Errors }),
                _ => Results.Problem("Unexpected result.")
            };
        })
        .WithName("DeliverOrder")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);


        orders.MapPost("/{id}/cancel", (string id, string reason, OrderService service) =>
        {
            var result = service.Cancel(id, reason);

            return result.Value switch
            {
                Success<Order> s => Results.Ok(s.Value),
                NotFound => Results.NotFound(),
                ValidationError ve => Results.BadRequest(new { errors = ve.Errors }),
                _ => Results.Problem("Unexpected result.")
            };
        })
        .WithName("CancelOrder")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
