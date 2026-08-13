# .NET 11 Discriminated Unions

A minimal ASP.NET Core Web API demonstrating **C# 15 union types**, an experimental feature in .NET 11.

## Requirements

* .NET 11 SDK (`11.0.100-preview.5` or later)
* C# preview features enabled

```xml
<TargetFramework>net11.0</TargetFramework>
<LangVersion>preview</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

## What This Demonstrates

### Union types

Define a closed set of possible types:

```csharp
public union OrderState(Pending, Confirmed, Shipped, Delivered, Cancelled);
```

Each case contains only the data relevant to that state:

```csharp
public record class Pending(DateTimeOffset CreatedAt);
public record class Confirmed(DateTimeOffset ConfirmedAt);
public record class Shipped(DateTimeOffset ShippedAt, string TrackingNumber);
public record class Delivered(DateTimeOffset DeliveredAt);
public record class Cancelled(DateTimeOffset CancelledAt, string Reason);
```
## API Results

The API also models expected outcomes as a union:

```csharp
public union OrderResult<T>(
    Success<T>,
    NotFound,
    ValidationError,
    Conflict);
```

Endpoints map each case to an HTTP response:

```csharp
return result.Value switch
{
    Success<Order> s => Results.Ok(s.Value),
    NotFound => Results.NotFound(),
    ValidationError e => Results.BadRequest(new { errors = e.Errors }),
    Conflict c => Results.Conflict(new { message = c.Message }),
};
```

## JSON

.NET 11 Preview 5 does not provide built-in union JSON serialization, so this project uses custom `System.Text.Json` converters.

Union values use a `type` discriminator:

```json
{
  "type": "shipped",
  "shippedAt": "...",
  "trackingNumber": "TRK-123"
}
```

API results use the same approach:

```json
{ "type": "not_found" }
```

```json
{
  "type": "validation_error",
  "errors": ["Tracking number must not be empty."]
}
```

The main project has **no third-party dependencies**.

## Why Unions?

The traditional approach often uses an enum plus nullable properties:

```csharp
enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

This allows invalid combinations such as a `Pending` order with a tracking number.

Unions make invalid states harder to represent:

* Each case contains only valid data.
* Pattern matching is exhaustive.
* Adding a case produces compiler errors at unhandled switches.
* Expected API outcomes can be modelled without exceptions.

## Run

```bash
dotnet build
dotnet run
```

Example requests:

```bash
curl http://localhost:5000/orders
curl http://localhost:5000/orders/ORD-001

curl -X POST http://localhost:5000/orders/ORD-001/confirm
curl -X POST "http://localhost:5000/orders/ORD-001/ship?trackingNumber=TRK-999"
curl -X POST http://localhost:5000/orders/ORD-001/deliver
curl -X POST "http://localhost:5000/orders/ORD-001/cancel?reason=changed+mind"
```

## Endpoints

| Method | Endpoint                             | Description         |
| ------ | ------------------------------------ | ------------------- |
| GET    | `/orders`                            | List orders         |
| GET    | `/orders/{id}`                       | Get an order        |
| POST   | `/orders`                            | Create an order     |
| POST   | `/orders/{id}/confirm`               | Pending → Confirmed |
| POST   | `/orders/{id}/ship?trackingNumber=X` | Confirmed → Shipped |
| POST   | `/orders/{id}/deliver`               | Shipped → Delivered |
| POST   | `/orders/{id}/cancel?reason=X`       | Cancel an order     |

Invalid state transitions return `400 Bad Request`.

## Tests

Run the test suite:

```bash
dotnet test --project tests/Tests.csproj
```

The test project uses **MSTest 4.0**. The API itself has no third-party NuGet dependencies.

Current verification:

```text
Passed! - Failed: 0, Passed: 52, Skipped: 0, Total: 52
```

To verify exhaustive matching during builds:

```bash
dotnet build -p:TreatWarningsAsErrors=true
```
