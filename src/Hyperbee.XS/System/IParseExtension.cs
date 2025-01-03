using System.Linq.Expressions;
using Parlot.Fluent;

namespace Hyperbee.XS.System;

public enum ExtensionType
{
    ComplexStatement,
    SingleStatement
}

public interface IParseExtension
{
    ExtensionType Type { get; }
    Parser<Expression> Parser( Parser<Expression> expression, Deferred<Expression> statement );
}
