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

public enum ExtensionType
{
    Complex,
    Terminated
    //Binary,
    //Unary
}

public interface IParseExtension
{
    ExtensionType Type { get; }
    KeyParserPair<Expression> CreateParser( ExtensionBinder binder );
}
