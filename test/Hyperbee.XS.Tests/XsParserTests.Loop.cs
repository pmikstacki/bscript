using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLoopTests
{
    public XsParser Xs { get; } = new();

    [TestMethod]
    public void Compile_ShouldSucceed_WithLoop()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            loop
            {
                x++; 
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

