using Hyperbee.XS.Parser;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class ExpressionScriptParserTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_Constant()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "5;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNegate()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "-1 + 3;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 2, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "!false;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot_Grouping()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "!(false);" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "10 + 12;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 22, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMultiple()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "10 + 12 + 14;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 36, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithGrouping()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "(10 + 12) * 2;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMulitpleGrouping()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "(10 + 12) * (1 + 1);" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryLessThan()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "10 < 11;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariable()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "var x = 10;" );

        var lambda = Lambda( expression );

        var compiled = lambda.Compile();

        Assert.IsNotNull( compiled );

        Assert.IsTrue( true );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndResult()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "var x = 10; x + 10;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 20, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndAssignmentResult()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "var x = 10; x = x + 10; x + 22;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAddAssignmentResult()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "var x = 10; x += 32;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPostResult()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "var x = 10; x++;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result ); // x++ returns the value before increment
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPrefixResult()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "var x = 10; ++x;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 11, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditional()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse(
        """
        if (true)
        {
            "hello";
        } 
        else
        { 
            "goodBye";
        }
        """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello", result );
    }
}
