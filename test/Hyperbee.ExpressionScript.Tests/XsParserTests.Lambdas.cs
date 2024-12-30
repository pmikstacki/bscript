using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLambdaTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithLambdaResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var myLambda = () => 1;
            myLambda();
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 1, result );
    }
}
