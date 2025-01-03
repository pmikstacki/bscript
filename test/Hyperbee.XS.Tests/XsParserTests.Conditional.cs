using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserConditionalTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithConditional()
    {
        var parser = new XsParser();
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

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalAndNoElse()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
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
        var parser = new XsParser();
        var expression = parser.Parse(
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
}

