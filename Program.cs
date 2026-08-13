var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(sp =>
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new OrderStateConverter(),
            new OrderResultConverter()
        }
    };
    return options;
});
builder.Services.AddSingleton<OrderService>();
var app = builder.Build();
var service = app.Services.GetRequiredService<OrderService>();
var now = DateTimeOffset.UtcNow;

service.Create(new Order("ORD-001",
    new OrderState(new Pending(now))));
service.Create(new Order("ORD-002",
    new OrderState(new Confirmed(now))));
service.Create(new Order("ORD-003",
    new OrderState(new Shipped(now, "TRACK-ABC-123"))));

app.MapOrderEndpoints();

await app.RunAsync();