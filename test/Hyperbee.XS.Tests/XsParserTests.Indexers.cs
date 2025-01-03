using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserIndexTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithIndexResult()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMultiDimensionalIndexResult()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x[32,10];
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithIndexChaining()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse(
            """
            new Hyperbee.XS.Tests.TestClass(-1)[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodChainingIndexResult()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse(
            """
            var x = new Hyperbee.XS.Tests.TestClass(-1);
            x.MethodThis()[42];
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}
