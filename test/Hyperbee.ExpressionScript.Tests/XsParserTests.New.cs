using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.Parsers;

namespace Hyperbee.XS.Tests;

public class SimpleClass
{
    public int Value { get; }

    public SimpleClass( int value )
    {
        Value = value;
    }
}

[TestClass]
public class XsParserNewExpressionTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithNewExpression()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse(
            """
            new Hyperbee.XS.Tests.SimpleClass(42);
            """ );

        var lambda = Expression.Lambda<Func<SimpleClass>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsNotNull( result );
        Assert.AreEqual( 42, result.Value );
    }
}
