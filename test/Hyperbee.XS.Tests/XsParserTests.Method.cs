using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserMethodTests
{
    public XsParser Xs { get; set; } = new
    (
        new XsConfig { References = [Assembly.GetExecutingAssembly()] }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodCall()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.MethodValue();
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodCallArgs()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.AddNumbers(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGenericMethodCall()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.GenericAdd<int>(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGenericMethodCallTypeInference()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.GenericAdd(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodCallChaining()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.MethodThis().AddNumbers(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodCallPropertyChaining()
    {
        var expression = Xs.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(42);
            x.MethodThis().PropertyThis.MethodValue();
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithStaticMethodCallArgs()
    {
        var expression = Xs.Parse(
            """
            var x = Hyperbee.XS.Tests.TestClass.StaticAddNumbers(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}

