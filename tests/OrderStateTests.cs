using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using Dotnet11.Unions.Api.Domain;

namespace Dotnet11.Unions.Api.Tests;

[TestClass]
public class OrderStateTests
{
    private static JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestMethod]
    public void Pending_ImplicitConversion_UnwrapsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = new Pending(now);
        var state = new OrderState(pending);

        Assert.IsTrue(state.Value is Pending p && p.CreatedAt == now);
    }

    [TestMethod]
    public void Confirmed_ImplicitConversion_UnwrapsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var confirmed = new Confirmed(now);
        var state = new OrderState(confirmed);

        Assert.IsTrue(state.Value is Confirmed c && c.ConfirmedAt == now);
    }

    [TestMethod]
    public void Shipped_ImplicitConversion_UnwrapsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var shipped = new Shipped(now, "TRK-1");
        var state = new OrderState(shipped);

        Assert.IsTrue(state.Value is Shipped s && s.TrackingNumber == "TRK-1");
    }

    [TestMethod]
    public void Delivered_ImplicitConversion_UnwrapsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var delivered = new Delivered(now);
        var state = new OrderState(delivered);

        Assert.IsTrue(state.Value is Delivered);
    }

    [TestMethod]
    public void Cancelled_ImplicitConversion_UnwrapsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var cancelled = new Cancelled(now, "reason");
        var state = new OrderState(cancelled);

        Assert.IsTrue(state.Value is Cancelled ca && ca.Reason == "reason");
    }

    [TestMethod]
    public void Serialize_Pending_ContainsTypeAndCreatedAt()
    {
        var state = new OrderState(new Pending(DateTimeOffset.UtcNow));
        string json = JsonSerializer.Serialize(state, typeof(OrderState), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");
        string flat = json.Replace("\n", "").Replace(" ", "");
        Assert.IsTrue(flat.Contains("\"type\":\"pending\""));
        Assert.IsTrue(flat.Contains("\"createdAt\":"));
    }

    [TestMethod]
    public void Serialize_Confirmed_ContainsTypeAndConfirmedAt()
    {
        var state = new OrderState(new Confirmed(DateTimeOffset.UtcNow));
        string json = JsonSerializer.Serialize(state, typeof(OrderState), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");
        string flat = json.Replace("\n", "").Replace(" ", "");
        Assert.IsTrue(flat.Contains("\"type\":\"confirmed\""));
        Assert.IsTrue(flat.Contains("\"confirmedAt\":"));
    }

    [TestMethod]
    public void Serialize_Shipped_ContainsTypeAndTrackingNumber()
    {
        var state = new OrderState(new Shipped(DateTimeOffset.UtcNow, "T"));
        string json = JsonSerializer.Serialize(state, typeof(OrderState), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");
        string flat = json.Replace("\n", "").Replace(" ", "");
        Assert.IsTrue(flat.Contains("\"type\":\"shipped\""));
        Assert.IsTrue(flat.Contains("\"trackingNumber\":"));
    }

    [TestMethod]
    public void Serialize_Delivered_ContainsTypeAndDeliveredAt()
    {
        var state = new OrderState(new Delivered(DateTimeOffset.UtcNow));
        string json = JsonSerializer.Serialize(state, typeof(OrderState), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");
        string flat = json.Replace("\n", "").Replace(" ", "");
        Assert.IsTrue(flat.Contains("\"type\":\"delivered\""));
        Assert.IsTrue(flat.Contains("\"deliveredAt\":"));
    }

    [TestMethod]
    public void Serialize_Cancelled_ContainsTypeAndReason()
    {
        var state = new OrderState(new Cancelled(DateTimeOffset.UtcNow, "r"));
        string json = JsonSerializer.Serialize(state, typeof(OrderState), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");
        string flat = json.Replace("\n", "").Replace(" ", "");
        Assert.IsTrue(flat.Contains("\"type\":\"cancelled\""));
        Assert.IsTrue(flat.Contains("\"reason\":"));
    }

    [TestMethod]
    public void Deserialize_Pending_RoundTrips()
    {
        const string json = """{"type":"pending","createdAt":"2026-08-14T00:00:00Z"}""";
        var result = JsonSerializer.Deserialize<OrderState>(json, s_jsonOpts);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(typeof(Pending), result.Value.GetType());
    }

    [TestMethod]
    public void Deserialize_Confirmed_RoundTrips()
    {
        const string json = """{"type":"confirmed","confirmedAt":"2026-08-14T00:00:00Z"}""";
        var result = JsonSerializer.Deserialize<OrderState>(json, s_jsonOpts);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(typeof(Confirmed), result.Value.GetType());
    }

    [TestMethod]
    public void Deserialize_Shipped_RoundTrips()
    {
        const string json = """{"type":"shipped","shippedAt":"2026-08-14T00:00:00Z","trackingNumber":"T1"}""";
        var result = JsonSerializer.Deserialize<OrderState>(json, s_jsonOpts);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(typeof(Shipped), result.Value.GetType());
    }

    [TestMethod]
    public void Deserialize_Delivered_RoundTrips()
    {
        const string json = """{"type":"delivered","deliveredAt":"2026-08-14T00:00:00Z"}""";
        var result = JsonSerializer.Deserialize<OrderState>(json, s_jsonOpts);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(typeof(Delivered), result.Value.GetType());
    }

    [TestMethod]
    public void Deserialize_Cancelled_RoundTrips()
    {
        const string json = """{"type":"cancelled","cancelledAt":"2026-08-14T00:00:00Z","reason":"r"}""";
        var result = JsonSerializer.Deserialize<OrderState>(json, s_jsonOpts);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(typeof(Cancelled), result.Value.GetType());
    }
}
