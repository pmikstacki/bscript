using Hyperbee.ExpressionScript.Parser;

using static System.Linq.Expressions.Expression;

namespace Hyperbee.ExpressionScript.Tests;

[TestClass]
public class ExpressionScriptParserTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithScript()
    {
        var parser = new ExpressionScriptParser();
        var expression = parser.Parse( "var x = 10;" );

        var lambda = Lambda( expression );

        var compiled = lambda.Compile();

        Assert.IsNotNull( compiled );

        Assert.IsTrue( true );
    }
}
