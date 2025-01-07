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

public class ForParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Complex;

    public KeyParserPair<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, assignable, statement) = binder;

        return new( "for",
            XsParsers.Bounded(
                static ctx =>
                {
                    var (scope, _) = ctx;
                    scope.Push( FrameType.Child );
                },
                Between(
                    Terms.Char( '(' ),
                        assignable.AndSkip( Terms.Char( ';' ) )
                            .And( expression ).AndSkip( Terms.Char( ';' ) )
                            .And( expression ),
                    Terms.Char( ')' )
                )
                .And(
                    Between(
                        Terms.Char( '{' ),
                        ZeroOrMany( statement ),
                        Terms.Char( '}' )
                    )
                )
                .Then<Expression>( static (ctx, parts) =>
                {
                    var (scope, _) = ctx;
                    var ((initializer, test, iteration), body) = parts;

                    var variables = scope.Variables.EnumerateValues( KeyScope.Current ).ToArray();

                    var bodyBlock = Block( body );
                    return ExpressionExtensions.For( variables, initializer, test, iteration, bodyBlock );
                } ),
                static ctx =>
                {
                    var (scope, _) = ctx;
                    scope.Pop();
                }
            )
        );
    }
}
