namespace Dotnet11.Unions.Api.Domain;

public record class Success<T>(T Value);

[JsonConverter(typeof(OrderResultConverter))]
public union OrderResult<T>(Success<T>, NotFound, ValidationError, Conflict);
