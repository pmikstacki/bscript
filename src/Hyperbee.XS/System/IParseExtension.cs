using System.Linq.Expressions;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;

namespace Hyperbee.XS.System;

public record ExtensionBinder(
    XsConfig Config,
    Parser<Expression> ExpressionParser,
    Parser<Expression> AssignableParser,
    Deferred<Expression> StatementParser
);

[Flags]
public enum ExtensionType
{
    None = 0,
    Literal = 1,
    Complex = 2,
    Terminated = 4,
    Binary = 8,
    //Unary = 16,
}

public interface IParseExtension
{
    ExtensionType Type { get; }
    string Key { get; }
    Parser<Expression> CreateParser( ExtensionBinder binder );
}
