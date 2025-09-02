using System.Linq.Expressions;

namespace bscript.Core.Writer;

public interface IXsWriter
{
    bool CanWrite( Expression node );
    void WriteExpression( Expression node, XsWriterContext context );
}
