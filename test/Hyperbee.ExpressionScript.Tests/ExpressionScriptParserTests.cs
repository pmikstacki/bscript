using Hyperbee.ExpressionScript.Parser;

using static System.Linq.Expressions.Expression;

namespace Hyperbee.ExpressionScript.Tests;

[TestClass]
public class ExpressionScriptParserTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithScript()
    {
        var expression = ExpressionScriptParser.Parse( "let x = 10" );

        var compiled = Lambda( expression );

        var r = compiled.Compile();
        
        Assert.IsNotNull( r );

        Assert.IsTrue( true );
    }
}
