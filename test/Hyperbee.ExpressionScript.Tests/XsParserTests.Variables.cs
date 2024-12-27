using Hyperbee.XS.Parsers;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserVariableTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithVariable()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10;" );

        var lambda = Lambda( expression );

        var compiled = lambda.Compile();

        Assert.IsNotNull( compiled );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x + 10;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 20, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndAssignmentResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x = x + 10; x + 22;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAddAssignmentResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x += 32;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPostResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x++;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result ); // x++ returns the value before increment
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPrefixResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; ++x;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 11, result );
    }
}

