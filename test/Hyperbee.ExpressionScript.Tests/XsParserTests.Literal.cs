using Hyperbee.XS.Parsers;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;


[TestClass]
public class XsParserLiteralTests
{
    [TestMethod]
    public void Parse_ShouldSucceed_WithIntegerLiteralDefault()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 12345;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithIntegerLiteral()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 12345N;" );
        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithLongLiteral()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 12345L;" );
        var lambda = Lambda<Func<long>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 12345L, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithFloatLiteral()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 123.45F;" );
        var lambda = Lambda<Func<float>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45F, result );
    }

    [TestMethod]
    public void Parse_ShouldSucceed_WithDoubleLiteral()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 123.45D;" );
        var lambda = Lambda<Func<double>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 123.45D, result );
    }
}

