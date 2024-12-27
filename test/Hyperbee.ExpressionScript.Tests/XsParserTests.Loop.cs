using Hyperbee.XS.Parsers;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLoopTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithLoop()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 0;
            loop
            {
                x++; // do something
                if( x == 10 )
                {
                    break;
                }
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result );
    }
}

