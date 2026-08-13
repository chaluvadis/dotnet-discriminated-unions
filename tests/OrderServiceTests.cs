using Dotnet11.Unions.Api.Application;
using Dotnet11.Unions.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dotnet11.Unions.Api.Tests;

[TestClass]
public class OrderServiceTests
{
    private static OrderService s_service = new();
    private static DateTimeOffset s_now = DateTimeOffset.UtcNow;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {

        s_service.Create(new Order("T-1", new OrderState(new Pending(s_now))));
        s_service.Create(new Order("T-2", new OrderState(new Confirmed(s_now))));
        s_service.Create(new Order("T-3", new OrderState(new Shipped(s_now, "TRK"))));
    }

    [TestMethod]
    public void Create_ReturnsSuccess()
    {
        var result = s_service.Create(new Order("C-1", new OrderState(new Pending(s_now))));
        Assert.IsTrue(result.Value is Success<Order>);
    }

    [TestMethod]
    public void Create_Duplicate_ReturnsConflict()
    {
        var result = s_service.Create(new Order("T-1", new OrderState(new Pending(s_now))));
        Assert.IsTrue(result.Value is Conflict);
    }

    [TestMethod]
    public void Get_Existing_ReturnsSuccess()
    {
        var result = s_service.Get("T-1");
        Assert.IsTrue(result.Value is Success<Order>);
    }

    [TestMethod]
    public void Get_Missing_ReturnsNotFound()
    {
        var result = s_service.Get("NONEXISTENT");
        Assert.IsTrue(result.Value is NotFound);
    }

    [TestMethod]
    public void Confirm_PendingToConfirmed_Succeeds()
    {
        var result = s_service.Confirm("T-1");
        Assert.IsTrue(result.Value is Success<Order> o && o.Value.State.Value is Confirmed);
    }

    [TestMethod]
    public void Confirm_AlreadyConfirmed_ReturnsValidationError()
    {
        var result = s_service.Confirm("T-2");
        Assert.IsTrue(result.Value is ValidationError);
    }

    [TestMethod]
    public void Ship_EmptyTracking_ReturnsValidationError()
    {
        var result = s_service.Ship("T-2", "");
        Assert.IsTrue(result.Value is ValidationError);
    }

    [TestMethod]
    public void Ship_ConfirmedToShipped_Succeeds()
    {
        var result = s_service.Ship("T-2", "TRK-42");
        Assert.IsTrue(result.Value is Success<Order> o && o.Value.State.Value is Shipped s && s.TrackingNumber == "TRK-42");
    }

    [TestMethod]
    public void Deliver_ShippedToDelivered_Succeeds()
    {
        var result = s_service.Deliver("T-2");
        Assert.IsTrue(result.Value is Success<Order> o && o.Value.State.Value is Delivered);
    }

    [TestMethod]
    public void Cancel_Delivered_ReturnsValidationError()
    {
        var result = s_service.Cancel("T-2", "reason");
        Assert.IsTrue(result.Value is ValidationError);
    }

    [TestMethod]
    public void Cancel_Pending_Succeeds()
    {
        var result = s_service.Cancel("T-3", "changed mind");
        Assert.IsTrue(result.Value is Success<Order> o && o.Value.State.Value is Cancelled ca && ca.Reason == "changed mind");
    }

    [TestMethod]
    public void Cancel_EmptyReason_ReturnsValidationError()
    {
        var result = s_service.Cancel("T-3", "");
        Assert.IsTrue(result.Value is ValidationError);
    }

    [TestMethod]
    public void GetAll_ReturnsAllOrders()
    {
        var all = s_service.GetAll();
        Assert.IsTrue(all.Length >= 3, $"Expected >= 3 orders, got {all.Length}");
    }
}
