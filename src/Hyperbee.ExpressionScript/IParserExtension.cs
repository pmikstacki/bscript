using System.Linq.Expressions;
using Parlot.Fluent;

namespace Hyperbee.XS;

public record XsContext( TypeResolver Resolver, ParseScope Scope, Parser<Expression> ExpressionParser, Deferred<Expression> StatementParser );

public enum ExtensionType
{
    ComplexStatement,
    SingleStatement
}

public interface IParserExtension
{
    ExtensionType Type { get; }
    Parser<Expression> Parser( XsContext xsContext );
}
