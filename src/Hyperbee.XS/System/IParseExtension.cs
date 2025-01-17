using System.Linq.Expressions;
using Parlot.Fluent;

namespace Hyperbee.XS.System;

public record ExtensionBinder(
    XsConfig Config,
    Parser<Expression> ExpressionParser,
    Parser<Expression> DeclarationParser,
    Deferred<Expression> StatementParser
);

[Flags]
public enum ExtensionType
{
    None = 0,
    //Primary = 1 << 0,
    Literal = 1 << 1,
    Expression = 1 << 2,
    Terminated = 1 << 3,
    //Binary = 1 << 4,
    //Unary = 1 << 5,
}

public interface IParseExtension
{
    ExtensionType Type { get; }
    string Key { get; }
    Parser<Expression> CreateParser( ExtensionBinder binder );
}
