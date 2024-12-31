using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserPropertyTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithPropertyResult()
    {
        var parser = new XsParser { References = [Assembly.GetExecutingAssembly()] };

        var expression = parser.Parse(
            """
            var x = new Hyperbee.XS.Tests.SimpleClass(42);
            x.Value;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}
