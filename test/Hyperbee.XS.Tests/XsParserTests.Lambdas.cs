using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLambdaTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithResult()
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
    public void Compile_ShouldSucceed_WithArgumentAndResult()
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
    public void Compile_ShouldSucceed_WitStatementArgument()
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

    [TestMethod]
    public void Compile_ShouldSucceed_WithReferenceArgument()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
            """
            var myLambda = ( Hyperbee.XS.Tests.TestClass x ) => { return x.PropertyValue; };
            var myClass = new Hyperbee.XS.Tests.TestClass(42);
            myLambda( myClass );
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithMethodChaining()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
            """
            var myLambda = ( int x ) => { return x + 1; };
            (myLambda( 41 )).ToString();
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "42", result );
    }
}
