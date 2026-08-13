namespace Dotnet11.Unions.Api.Serialization;

public sealed class OrderResultConverter : JsonConverter<OrderResult<Order>>
{
    private static readonly IReadOnlyDictionary<string, Func<string, JsonSerializerOptions, OrderResult<Order>>> CaseReaders
        = new Dictionary<string, Func<string, JsonSerializerOptions, OrderResult<Order>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["not_found"] = (raw, opts) =>
            {
                var nf = JsonSerializer.Deserialize<NotFound>(raw, opts)
                    ?? throw new JsonException("Unable to deserialize NotFound.");
                return new OrderResult<Order>(nf);
            },
            ["validation_error"] = (raw, opts) =>
            {
                var ve = JsonSerializer.Deserialize<ValidationError>(raw, opts)
                    ?? throw new JsonException("Unable to deserialize ValidationError.");
                return new OrderResult<Order>(ve);
            },
            ["conflict"] = (raw, opts) =>
            {
                var c = JsonSerializer.Deserialize<Conflict>(raw, opts)
                    ?? throw new JsonException("Unable to deserialize Conflict.");
                return new OrderResult<Order>(c);
            }
        };

    public override OrderResult<Order> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;


        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("type", out var typeProp)
            && typeProp.GetString() is { Length: > 0 } typeValue
            && CaseReaders.TryGetValue(typeValue, out var caseReader))
        {
            return caseReader(root.GetRawText(), options);
        }


        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("id", out _)
            && root.TryGetProperty("state", out _))
        {
            var order = JsonSerializer.Deserialize<Order>(root.GetRawText(), options)
                ?? throw new JsonException("Unable to deserialize Order.");
            return new OrderResult<Order>(new Success<Order>(order));
        }

        throw new JsonException(
            "Unable to determine OrderResult case from JSON. " +
            "Expected 'type' discriminator or an Order object with 'id' and 'state' properties.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        OrderResult<Order> value,
        JsonSerializerOptions options)
    {

        if (value.Value is Success<Order> success)
        {
            JsonSerializer.Serialize(writer, success.Value, typeof(Order), options);
            return;
        }

        if (value.Value is NotFound nf)
        {
            JsonSerializer.Serialize(writer, new { type = "not_found" }, options);
            return;
        }

        if (value.Value is ValidationError ve)
        {
            JsonSerializer.Serialize(writer, new { type = "validation_error", errors = ve.Errors }, options);
            return;
        }

        if (value.Value is Conflict c)
        {
            JsonSerializer.Serialize(writer, new { type = "conflict", message = c.Message }, options);
            return;
        }

        if (value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        throw new JsonException(
            $"Unhandled OrderResult case: {value.Value.GetType().Name}.");
    }
}
