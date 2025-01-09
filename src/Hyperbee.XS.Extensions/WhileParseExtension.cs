using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class WhileParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Complex;
    public string Key => "while";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, assignable, statement) = binder;

        return
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
            } ).Named( "while" );
    }
}
