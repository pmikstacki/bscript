using System.Linq.Expressions;
using Hyperbee.Collections;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class WhileParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Complex;

    public KeyParserPair<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, assignable, statement) = binder;

        return new( "while",
            Between(
                Terms.Char( '(' ),
                expression,
                Terms.Char( ')' )
            )
            .And(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( statement ),
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                var (test, body) = parts;

                var bodyBlock = Block( body );
                return ExpressionExtensions.While( test, bodyBlock );
            } )
        );
    }
}
