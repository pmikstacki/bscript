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
                    var (scope, _) = ctx;
                    scope.Push( FrameType.Block );
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
                    var (scope, _) = ctx;
                    var ((initializer, test, iteration), body) = parts;

                    // Call ToArray to ensure the variables remain in scope for reduce.
                    var variables = scope.Variables.EnumerateValues( KeyScope.Current ).ToArray();

                    return ExpressionExtensions.For( variables, initializer, test, iteration, body );
                } ),
                static ctx =>
                {
                    var (scope, _) = ctx;
                    scope.Pop();
                }
            ).Named( "for" );
    }
}
