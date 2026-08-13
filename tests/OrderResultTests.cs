using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using Dotnet11.Unions.Api.Domain;

namespace Dotnet11.Unions.Api.Tests;

[TestClass]
public class OrderResultTests
{
    private static JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestMethod]
    public void Success_ContainsOrder()
    {
        var order = new Order("O1", new OrderState(new Pending(DateTimeOffset.UtcNow)));
        var result = new OrderResult<Order>(new Success<Order>(order));

        Assert.IsTrue(result.Value is Success<Order> s && s.Value.Id == "O1");
    }

    [TestMethod]
    public void NotFound_IsCorrectType()
    {
        var result = new OrderResult<Order>(new NotFound());
        Assert.IsTrue(result.Value is NotFound);
    }

    [TestMethod]
    public void ValidationError_ContainsErrors()
    {
        string[] errors = ["err1", "err2"];
        var result = new OrderResult<Order>(new ValidationError(errors));

        Assert.IsTrue(result.Value is ValidationError ve && ve.Errors.Length == 2);
    }

    [TestMethod]
    public void Conflict_ContainsMessage()
    {
        var result = new OrderResult<Order>(new Conflict("msg"));
        Assert.IsTrue(result.Value is Conflict co && co.Message == "msg");
    }

    private static string Normalize(string json) => json.Replace("\n", "").Replace(" ", "");

    [TestMethod]
    public void Serialize_Success_TransparentUnwrap()
    {
        var order = new Order("ORD-X", new OrderState(new Pending(DateTimeOffset.UtcNow)));
        var result = new OrderResult<Order>(new Success<Order>(order));

        string json = JsonSerializer.Serialize(result, typeof(OrderResult<Order>), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");

        string flat = Normalize(json);
        Assert.IsTrue(flat.Contains("\"id\":\"ORD-X\""));
        Assert.IsTrue(flat.Contains("\"state\""));
    }

    [TestMethod]
    public void Serialize_NotFound_ContainsTypeDiscriminator()
    {
        var result = new OrderResult<Order>(new NotFound());
        string json = JsonSerializer.Serialize(result, typeof(OrderResult<Order>), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");

        string flat = Normalize(json);
        Assert.IsTrue(flat.Contains("\"type\":\"not_found\""));
    }

    [TestMethod]
    public void Serialize_ValidationError_ContainsErrors()
    {
        string[] errors = ["e1", "e2"];
        var result = new OrderResult<Order>(new ValidationError(errors));
        string json = JsonSerializer.Serialize(result, typeof(OrderResult<Order>), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");

        string flat = Normalize(json);
        Assert.IsTrue(flat.Contains("\"type\":\"validation_error\""));
        Assert.IsTrue(flat.Contains("\"errors\""));
    }

    [TestMethod]
    public void Serialize_Conflict_ContainsMessage()
    {
        var result = new OrderResult<Order>(new Conflict("msg"));
        string json = JsonSerializer.Serialize(result, typeof(OrderResult<Order>), s_jsonOpts) as string
            ?? throw new AssertFailedException("Serialization returned null.");

        string flat = Normalize(json);
        Assert.IsTrue(flat.Contains("\"type\":\"conflict\""));
        Assert.IsTrue(flat.Contains("\"message\":\"msg\""));
    }

    [TestMethod]
    public void Deserialize_Success_RoundTrips()
    {
        const string json = """{"id":"ORD-X","state":{"type":"pending","createdAt":"2026-08-14T00:00:00Z"}}""";
        var result = JsonSerializer.Deserialize<OrderResult<Order>>(json, s_jsonOpts);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value is Success<Order> s && s.Value.Id == "ORD-X");
    }

    [TestMethod]
    public void Deserialize_NotFound_RoundTrips()
    {
        const string json = """{"type":"not_found"}""";
        var result = JsonSerializer.Deserialize<OrderResult<Order>>(json, s_jsonOpts);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value is NotFound);
    }

    [TestMethod]
    public void Deserialize_ValidationError_RoundTrips()
    {
        const string json = """{"type":"validation_error","errors":["e1","e2"]}""";
        var result = JsonSerializer.Deserialize<OrderResult<Order>>(json, s_jsonOpts);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value is ValidationError v && v.Errors.Length == 2);
    }

    [TestMethod]
    public void Deserialize_Conflict_RoundTrips()
    {
        const string json = """{"type":"conflict","message":"msg"}""";
        var result = JsonSerializer.Deserialize<OrderResult<Order>>(json, s_jsonOpts);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value is Conflict c && c.Message == "msg");
    }
}
