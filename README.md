# .NET 11 Discriminated Unions Showcase

A minimal ASP.NET Core Web API demonstrating **C# 15 union types** — an experimental feature in .NET 11.

## Feature

C# 15 introduces **union types**, letting you declare a value that is exactly one of a fixed set of types, with compiler-enforced exhaustive pattern matching:

```csharp
public union OrderState(Pending, Confirmed, Shipped, Delivered, Cancelled);
```

Each case carries only the data valid for that state. The compiler requires every case to be handled in a `switch` expression, emitting **CS8509** if any is missing.

## Prerequisites

```
.NET 11 SDK (11.0.100-preview.5 or later)
```

Verify: `dotnet --version`

## Enabling the Feature

Union types are a preview language feature. Opt-in in the `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

- **`<LangVersion>preview</LangVersion>`** enables the `union` keyword
- **`<TargetFramework>net11.0</TargetFramework>`** ensures `UnionAttribute` and `IUnion` are available in-box (Preview 5+)

> No separate polyfill is needed in Preview 5 — `UnionAttribute` and `IUnion` ship in `System.Private.CoreLib`.

## Union Declaration

Each case is a record type with state-specific data:

```csharp
// Domain/OrderStateCases.cs
public record class Pending(DateTimeOffset CreatedAt);
public record class Confirmed(DateTimeOffset ConfirmedAt);
public record class Shipped(DateTimeOffset ShippedAt, string TrackingNumber);
public record class Delivered(DateTimeOffset DeliveredAt);
public record class Cancelled(DateTimeOffset CancelledAt, string Reason);
```

The union composes these into a closed set:

```csharp
// Domain/OrderState.cs
[JsonConverter(typeof(OrderStateConverter))]
public union OrderState(Pending, Confirmed, Shipped, Delivered, Cancelled);
```

The compiler generates a `readonly struct` with one constructor per case type (enabling implicit conversion) and a `Value` property.

## Pattern Matching

Pattern matching unwraps the union automatically — you write `Pending p` and the compiler checks `Value`:

```csharp
string label = order.State.Value switch
{
    Pending p    => $"Pending since {p.CreatedAt}",
    Confirmed c  => $"Confirmed at {c.ConfirmedAt}",
    Shipped s    => $"Shipped: {s.TrackingNumber}",
    Delivered d  => $"Delivered at {d.DeliveredAt}",
    Cancelled ca => $"Cancelled: {ca.Reason}",
};
// Compiler error CS8509 if any case is missing
```

## API Result Modelling

Operations return a union of possible outcomes instead of throwing for expected conditions:

```csharp
// Domain/OrderResult.cs
public record class Success<T>(T Value);
public record class NotFound();
public record class ValidationError(string[] Errors);
public record class Conflict(string Message);

[JsonConverter(typeof(OrderResultConverter))]
public union OrderResult<T>(Success<T>, NotFound, ValidationError, Conflict);
```

Endpoints pattern-match exhaustively to produce HTTP responses:

```csharp
return result.Value switch
{
    Success<Order> s      => Results.Ok(s.Value),
    NotFound              => Results.NotFound(),
    ValidationError ve    => Results.BadRequest(new { errors = ve.Errors }),
    Conflict c            => Results.Conflict(new { message = c.Message }),
};
```

## Exhaustive Matching

A `switch` expression over a union is exhaustive when it handles all case types. The compiler verifies this at build time.

If you add a new case to the union, the compiler produces **CS8509** at every switch that doesn't handle it — forcing you to update every consumer before the code compiles.

With a traditional `enum + _ => default`, adding a new enum value compiles silently; the `_` arm swallows the new case. Unions eliminate that entire class of bug.

Verify exhaustiveness is active:

```bash
dotnet build -p:TreatWarningsAsErrors=true
```

## JSON Serialization

.NET 11 Preview 5 does not have built-in union JSON serialization (that arrived in Preview 6). This project implements it using only built-in `System.Text.Json` APIs via custom `JsonConverter`s — no Newtonsoft, no third-party packages.

**OrderState** serializes with a `"type"` discriminator:

```json
{ "type": "pending", "createdAt": "2026-08-14T00:00:00Z" }
{ "type": "shipped", "shippedAt": "...", "trackingNumber": "TRK-123" }
{ "type": "cancelled", "cancelledAt": "...", "reason": "customer request" }
```

**OrderResult** serializes `Success<T>` transparently; other cases use the discriminator:

```json
{"id":"ORD-001","state":{"type":"confirmed","confirmedAt":"..."}}
{"type":"not_found"}
{"type":"validation_error","errors":["Tracking number must not be empty."]}
{"type":"conflict","message":"Order already exists."}
```

## Traditional Approach vs. Unions

**Enum + nullable properties:**

```csharp
enum OrderStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }

class Order
{
    public OrderStatus Status { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CancellationReason { get; set; }
}
```

Problems: nullable fields in every state, no exhaustiveness, invalid combinations representable, new enum values silently swallowed by `_`.

**With unions:** each case carries only its valid data, pattern matching is exhaustive, and adding a new case breaks every unhandled switch at compile time.

## Project Structure

```
Dotnet11.Unions.Api/
├── Dotnet11.Unions.Api.csproj
├── Program.cs
├── Domain/
│   ├── OrderStateCases.cs       # Pending, Confirmed, Shipped, Delivered, Cancelled
│   ├── OrderState.cs            # public union OrderState(...)
│   ├── OrderResultCases.cs      # Success<T>, NotFound, ValidationError, Conflict
│   ├── OrderResult.cs           # public union OrderResult<T>(...)
│   └── Order.cs                 # record Order
├── Application/OrderService.cs  # In-memory store; state transitions
├── Api/OrderEndpoints.cs        # Minimal API endpoints; union → HTTP mapping
├── Serialization/
│   ├── OrderStateConverter.cs   # JsonConverter with "type" discriminator
│   └── OrderResultConverter.cs  # JsonConverter for OrderResult<T>
├── tests/
│   ├── Tests.csproj                    # MSTest 4.0 test project
│   ├── OrderStateTests.cs             # Union creation, serialization, deserialization
│   ├── OrderResultTests.cs            # OrderResult<T> creation, serialization, deserialization
│   ├── OrderServiceTests.cs           # Service behavior and state transitions
│   └── ExhaustivenessTests.cs         # Compile-time exhaustiveness verification
└── README.md
```

## Running

```bash
# Build
dotnet build

# Run the API
dotnet run

# Exercise endpoints
curl http://localhost:5000/orders
curl http://localhost:5000/orders/ORD-001
curl -X POST http://localhost:5000/orders/ORD-001/confirm
curl -X POST http://localhost:5000/orders/ORD-001/ship?trackingNumber=TRK-999
curl -X POST http://localhost:5000/orders/ORD-001/deliver
curl -X POST "http://localhost:5000/orders/ORD-001/cancel?reason=changed+mind"

# Run verification tests with MSTest
dotnet test --project tests/Tests.csproj
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/orders` | List all orders |
| GET | `/orders/{id}` | Get order by ID (404 if missing) |
| POST | `/orders` | Create order (201 Created) |
| POST | `/orders/{id}/confirm` | Pending → Confirmed |
| POST | `/orders/{id}/ship?trackingNumber=X` | Confirmed → Shipped |
| POST | `/orders/{id}/deliver` | Shipped → Delivered |
| POST | `/orders/{id}/cancel?reason=X` | Pending/Confirmed/Shipped → Cancelled |

Invalid transitions return `400 Bad Request` with a `ValidationError`.

## Dependencies

The **main API project** (`Dotnet11.Unions.Api.csproj`) uses **only .NET 11 / ASP.NET Core APIs** and has **no third-party NuGet dependencies**.

The **test project** (`tests/Tests.csproj`) uses **MSTest 4.0** (`Microsoft.TestPlatform` / `MSTest.TestFramework`) for structured test discovery, assertions, and `dotnet test` integration. This is the only NuGet package in the repository, and it is confined to the test project.

## Tests

52 MSTest checks organized into 4 test classes:

| Class | Checks |
|-------|--------|
| `OrderStateTests` | Union creation, JSON serialization (5), JSON deserialization (5) |
| `OrderResultTests` | Creation (4), JSON serialization (4), JSON deserialization (4) |
| `OrderServiceTests` | Service behavior and state transitions (12) |
| `ExhaustivenessTests` | Compile-time exhaustiveness reflection checks (5) |

The test project references **MSTest 4.0** for structured test discovery, `[TestMethod]` / `[DataTestMethod]` attributes, and `Assert` helpers. The main API project remains free of third-party packages.

```bash
dotnet test --project tests/Tests.csproj
# Passed!  - Failed: 0, Passed: 52, Skipped: 0, Total: 52
```

## SDK Verification

```bash
$ dotnet --version
11.0.100-preview.5.26302.115

$ dotnet build
Build succeeded. 0 Warning(s) 0 Error(s)

$ dotnet test --project tests/Tests.csproj
Passed!  - Failed: 0, Passed: 52, Skipped: 0, Total: 52
```
