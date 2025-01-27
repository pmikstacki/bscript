using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

public interface IExtensionWriter
{
    bool TryExpressionWriter( Expression node, ExpressionWriterContext context );
}
