using System.Linq.Expressions;
using Hyperbee.Collections;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class ForParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "for";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return
            XsParsers.Bounded(
                static ctx =>
                {
                    ctx.EnterScope( FrameType.Block );
                },
                Between(
                    Terms.Char( '(' ),
                    expression.AndSkip( Terms.Char( ';' ) )
                            .And( expression ).AndSkip( Terms.Char( ';' ) )
                            .And( expression ),
                    Terms.Char( ')' )
                )
                .And( statement )
                .Then<Expression>( static ( ctx, parts ) =>
                {
                    var ((initializer, test, iteration), body) = parts;

                    // Call ToArray to ensure the variables remain in scope for reduce.
                    var variables = ctx.Scope().Variables
                        .EnumerateValues( KeyScope.Current ).ToArray();

                    return ExpressionExtensions.For( variables, initializer, test, iteration, body );
                } ),
                static ctx =>
                {
                    ctx.ExitScope();
                }
            ).Named( "for" );
    }
}
