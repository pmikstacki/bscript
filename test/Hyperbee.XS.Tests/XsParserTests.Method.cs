using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserMethodTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodCall()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
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
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
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
    public void Compile_ShouldSucceed_WithMethodCallChaining()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
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
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
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
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
            """
            var x = Hyperbee.XS.Tests.TestClass.StaticAddNumbers(10,32);
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}

