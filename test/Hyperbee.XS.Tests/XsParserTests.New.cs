using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserNewExpressionTests
{
    public XsParser Xs { get; set; } = new
    (
        new XsConfig { References = [Assembly.GetExecutingAssembly()] }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewExpression()
    {
        var expression = Xs.Parse(
            """
            new Hyperbee.XS.Tests.TestClass(42);
            """ );

        var lambda = Expression.Lambda<Func<TestClass>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsNotNull( result );
        Assert.AreEqual( 42, result.PropertyValue );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewAndProperty()
    {
        try
        {
            var expression = Xs.Parse(
                """
                new Hyperbee.XS.Tests.TestClass(42).PropertyThis.PropertyValue;
                """ );

            var lambda = Expression.Lambda<Func<int>>( expression );

            var compiled = lambda.Compile();
            var result = compiled();

            Assert.AreEqual( 42, result );
        }
        catch ( SyntaxException se )
        {
            Assert.Fail( se.Message );
        }
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewArray()
    {
        var expression = Xs.Parse(
            """
            new int[5];
            """ );

        var lambda = Expression.Lambda<Func<int[]>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 5, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewMultiDimensionalArray()
    {
        var expression = Xs.Parse(
            """
            new int[2,5];
            """ );

        var lambda = Expression.Lambda<Func<int[,]>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result.Length );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithGeneric()
    {
        var expression = Xs.Parse(
            """
            new List<int>();
            """ );

        var lambda = Expression.Lambda<Func<List<int>>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsInstanceOfType<List<int>>( result );
    }
}
