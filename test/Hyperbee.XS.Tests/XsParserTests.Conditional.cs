using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserConditionalTests
{
    public XsParser Xs { get; } = new();

    [TestMethod]
    public void Compile_ShouldSucceed_Please()
    {
        var expression = Xs.Parse(
            """
            var x = if (true)
                1;
            else
                2;
            
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 1, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditional()
    {
        var expression = Xs.Parse(
            """
            if (true)
            //{
                "hello";
            //} 
            else
            //{ 
                "goodBye";
            //}
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalAndNoElse()
    {
        var expression = Xs.Parse(
            """
            var x = "goodbye";
            if (true)
            {
                x = "hello";
            }
            x; 
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalVariable()
    {
        var expression = Xs.Parse(
            """
            var x = 10;
            if ( x == (9 + 1) )
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

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalAssignment()
    {
        var expression = Xs.Parse(
            """
            var result = if (true)
            {
                "hello";
            } 
            else
            { 
                "goodBye";
            }
            result;
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello", result );
    }
}

