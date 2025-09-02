using System.Linq.Expressions;

namespace bscript.Core.Writer;

public interface IExpressionWriter
{
    bool CanWrite( Expression node );
    void WriteExpression( Expression node, ExpressionWriterContext context );
}
