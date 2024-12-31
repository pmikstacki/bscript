using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserNewExpressionTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithNewExpression()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse( "new Hyperbee.XS.Tests.SimpleClass(42);" );

        var lambda = Expression.Lambda<Func<SimpleClass>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsNotNull( result );
        Assert.AreEqual( 42, result.Value );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithNewAndProperty()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse(
            """
            new Hyperbee.XS.Tests.SimpleClass(42).Value;
            """ );

        var lambda = Expression.Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}
