using System.Linq.Expressions;
using Parlot.Fluent;

namespace Hyperbee.XS.System;

public record ExtensionBinder( Parser<Expression> ExpressionParser, Parser<Expression> AssignableParser, Deferred<Expression> StatementParser );

public enum ExtensionType
{
    ComplexStatement,
    SingleStatement
}

public interface IParseExtension
{
    ExtensionType Type { get; }
    Parser<Expression> Parser( ExtensionBinder binder );
}
