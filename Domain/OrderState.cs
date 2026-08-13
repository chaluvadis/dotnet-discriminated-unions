using Dotnet11.Unions.Api.Serialization;

namespace Dotnet11.Unions.Api.Domain;

[JsonConverter(typeof(OrderStateConverter))]
public union OrderState(Pending, Confirmed, Shipped, Delivered, Cancelled);
