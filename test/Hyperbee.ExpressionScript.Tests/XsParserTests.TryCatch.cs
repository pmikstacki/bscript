using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserTryCatchTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithTryCatch()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 0;
            try
            {
                x = 32; // do something
            }
            catch( Exception e )
            {
                x -= 10;
            }
            finally
            {
                x+= 10;
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}
