using System.Globalization;
using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

public static class ExpressionTreeExtensions
{
    public static string ToExpressionTreeString( this Expression expression, ExpressionTreeVisitorConfig config = null )
    {
        using var output = new StringWriter( CultureInfo.CurrentCulture );
        ExpressionTreeVisitor.WriteTo( expression, output, config );
        return output.ToString();
    }

    public static void ToExpressionTreeString( this Expression expression, StringWriter output, ExpressionTreeVisitorConfig config = null )
    {
        ExpressionTreeVisitor.WriteTo( expression, output, config );
    }
}
