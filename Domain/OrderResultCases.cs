namespace Dotnet11.Unions.Api.Domain;

public record class NotFound();
public record class ValidationError(string[] Errors);
public record class Conflict(string Message);
