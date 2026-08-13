using System.Reflection;
using System.Runtime.CompilerServices;
using Dotnet11.Unions.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dotnet11.Unions.Api.Tests;

[TestClass]
public class ExhaustivenessTests
{
    [TestMethod]
    public void OrderState_HasUnionAttribute()
    {
        var asm = typeof(OrderState).Assembly;
        bool found = false;
        foreach (var t in asm.GetTypes())
        {
            if (t.GetCustomAttributes(typeof(UnionAttribute), false).Length > 0)
            {
                found = true;
                break;
            }
        }
        Assert.IsTrue(found, "No type with UnionAttribute found in the compiled assembly.");
    }

    [TestMethod]
    public void OrderState_HasConstructorForPending() => AssertHasConstructor(typeof(OrderState), typeof(Pending), "Pending");

    [TestMethod]
    public void OrderState_HasConstructorForConfirmed() => AssertHasConstructor(typeof(OrderState), typeof(Confirmed), "Confirmed");

    [TestMethod]
    public void OrderState_HasConstructorForShipped() => AssertHasConstructor(typeof(OrderState), typeof(Shipped), "Shipped");

    [TestMethod]
    public void OrderState_HasConstructorForDelivered() => AssertHasConstructor(typeof(OrderState), typeof(Delivered), "Delivered");

    [TestMethod]
    public void OrderState_HasConstructorForCancelled() => AssertHasConstructor(typeof(OrderState), typeof(Cancelled), "Cancelled");

    [TestMethod]
    public void OrderResult_HasConstructorForSuccess()
    {
        var openType = typeof(OrderResult<>);
        var closedType = openType.MakeGenericType(typeof(Order));
        AssertHasConstructor(closedType, typeof(Success<Order>), "Success<Order>");
    }

    [TestMethod]
    public void OrderResult_HasConstructorForNotFound()
    {
        var openType = typeof(OrderResult<>);
        var closedType = openType.MakeGenericType(typeof(Order));
        AssertHasConstructor(closedType, typeof(NotFound), "NotFound");
    }

    [TestMethod]
    public void OrderResult_HasConstructorForValidationError()
    {
        var openType = typeof(OrderResult<>);
        var closedType = openType.MakeGenericType(typeof(Order));
        AssertHasConstructor(closedType, typeof(ValidationError), "ValidationError");
    }

    [TestMethod]
    public void OrderResult_HasConstructorForConflict()
    {
        var openType = typeof(OrderResult<>);
        var closedType = openType.MakeGenericType(typeof(Order));
        AssertHasConstructor(closedType, typeof(Conflict), "Conflict");
    }

    [TestMethod]
    public void OrderState_HasAtLeastFiveConstructors()
    {
        var count = typeof(OrderState).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length;
        Assert.IsTrue(count >= 5, $"Expected >= 5 constructors, got {count}.");
    }

    [TestMethod]
    public void OrderResult_HasAtLeastFourConstructors()
    {
        var openType = typeof(OrderResult<>);
        var closedType = openType.MakeGenericType(typeof(Order));
        var count = closedType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length;
        Assert.IsTrue(count >= 4, $"Expected >= 4 constructors, got {count}.");
    }

    private static void AssertHasConstructor(Type unionType, Type caseType, string caseName)
    {
        var ctors = unionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        bool found = false;
        foreach (var ctor in ctors)
        {
            var p = ctor.GetParameters();
            if (p.Length == 1 && p[0].ParameterType == caseType)
            {
                found = true;
                break;
            }
        }
        Assert.IsTrue(found, $"No constructor for case type {caseName} found on {unionType.Name}.");
    }
}
