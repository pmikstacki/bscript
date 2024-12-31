using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLambdaTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithLambdaResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var myLambda = () => 1;
            myLambda();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 1, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithLambdaArgumentAndResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var myLambda = ( int x ) => x;
            myLambda( 10 );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithLambdaStatementArgumentAndResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var myLambda = ( int x ) => { return x + 1; };
            myLambda( 12 );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 13, result );
    }
}
