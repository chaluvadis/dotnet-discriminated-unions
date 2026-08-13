namespace Dotnet11.Unions.Api.Serialization;

public sealed class OrderStateConverter : JsonConverter<OrderState>
{
    private static readonly IReadOnlyDictionary<string, Func<string, JsonSerializerOptions, OrderState>> CaseReaders
        = new Dictionary<string, Func<string, JsonSerializerOptions, OrderState>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pending"] = (raw, opts) => ReadCase(raw, opts, "Pending"),
            ["confirmed"] = (raw, opts) => ReadCase(raw, opts, "Confirmed"),
            ["shipped"] = (raw, opts) => ReadCase(raw, opts, "Shipped"),
            ["delivered"] = (raw, opts) => ReadCase(raw, opts, "Delivered"),
            ["cancelled"] = (raw, opts) => ReadCase(raw, opts, "Cancelled"),
        };

    public override OrderState Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("Missing required 'type' discriminator property.");

        var typeValue = typeProp.GetString();
        if (string.IsNullOrEmpty(typeValue))
            throw new JsonException("'type' discriminator is null or empty.");

        if (!CaseReaders.TryGetValue(typeValue, out var caseReader))
            throw new JsonException($"Unknown OrderState type discriminator: '{typeValue}'.");

        return caseReader(root.GetRawText(), options);
    }

    public override void Write(
        Utf8JsonWriter writer,
        OrderState value,
        JsonSerializerOptions options)
    {
        object payload = value.Value switch
        {
            Pending p => new { type = "pending", createdAt = p.CreatedAt },
            Confirmed c => new { type = "confirmed", confirmedAt = c.ConfirmedAt },
            Shipped s => new { type = "shipped", shippedAt = s.ShippedAt, trackingNumber = s.TrackingNumber },
            Delivered d => new { type = "delivered", deliveredAt = d.DeliveredAt },
            Cancelled ca => new { type = "cancelled", cancelledAt = ca.CancelledAt, reason = ca.Reason },
            null => throw new JsonException("OrderState has no value (null)."),
            var unknown => throw new JsonException(
                $"Unhandled OrderState case: {unknown.GetType().Name}.")
        };

        JsonSerializer.Serialize(writer, payload, options);
    }

    private static OrderState ReadCase(string raw, JsonSerializerOptions options, string caseName)
    {
        return caseName switch
        {
            "Pending" => new OrderState(
                JsonSerializer.Deserialize<Pending>(raw, options)
                ?? throw new JsonException("Unable to deserialize Pending.")),

            "Confirmed" => new OrderState(
                JsonSerializer.Deserialize<Confirmed>(raw, options)
                ?? throw new JsonException("Unable to deserialize Confirmed.")),

            "Shipped" => new OrderState(
                JsonSerializer.Deserialize<Shipped>(raw, options)
                ?? throw new JsonException("Unable to deserialize Shipped.")),

            "Delivered" => new OrderState(
                JsonSerializer.Deserialize<Delivered>(raw, options)
                ?? throw new JsonException("Unable to deserialize Delivered.")),

            "Cancelled" => new OrderState(
                JsonSerializer.Deserialize<Cancelled>(raw, options)
                ?? throw new JsonException("Unable to deserialize Cancelled.")),

            _ => throw new JsonException($"Unknown OrderState case: {caseName}.")
        };
    }
}
