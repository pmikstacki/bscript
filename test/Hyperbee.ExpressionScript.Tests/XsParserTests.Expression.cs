using Hyperbee.XS.Parsers;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserExpressionTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_Constant()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "5;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNegate()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "-1 + 3;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 2, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "!false;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot_Grouping()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "!(false);" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "10 + 12;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 22, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMultiple()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "10 + 12 + 14;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 36, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithGrouping()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "(10 + 12) * 2;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMulitpleGrouping()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "(10 + 12) * (1 + 1);" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryLessThan()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "10 < 11;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }
}

