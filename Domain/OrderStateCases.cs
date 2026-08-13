namespace Dotnet11.Unions.Api.Domain;

public record class Pending(DateTimeOffset CreatedAt);

public record class Confirmed(DateTimeOffset ConfirmedAt);

public record class Shipped(DateTimeOffset ShippedAt, string TrackingNumber);

public record class Delivered(DateTimeOffset DeliveredAt);

public record class Cancelled(DateTimeOffset CancelledAt, string Reason);
