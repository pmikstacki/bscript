using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;


[TestClass]
public class XsParserLiteralTests
{
    public XsParser Xs { get; } = new();

    [TestMethod]
    public void Parse_ShouldSucceed_WithIntegerLiteralDefault()
    {
        var expression = Xs.Parse( "var x = 12345;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithIntegerLiteral()
    {
        var expression = Xs.Parse( "var x = 12345N;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongLiteral()
    {
        var expression = Xs.Parse( "var x = 12345L;" );
        var lambda = Lambda<Func<long>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345L, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithFloatLiteral()
    {
        var expression = Xs.Parse( "var x = 123.45F;" );
        var lambda = Lambda<Func<float>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45F, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithDoubleLiteral()
    {
        var expression = Xs.Parse( "var x = 123.45D;" );
        var lambda = Lambda<Func<double>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45D, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithDoubleLiteralResult()
    {
        var expression = Xs.Parse( "123.45D;" );
        var lambda = Lambda<Func<double>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45D, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLiteralMethodCallChaining()
    {
        var expression = Xs.Parse( "123.45D.ToString();" );
        var lambda = Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "123.45", result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLiteralGroupingMethodCallChaining()
    {
        var expression = Xs.Parse( "(123.45D + 7D).ToString();" );
        var lambda = Lambda<Func<string>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "130.45", result );
    }
}

