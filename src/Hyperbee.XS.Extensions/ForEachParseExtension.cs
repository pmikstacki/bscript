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

public class ForEachParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Complex;

    public KeyParserPair<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, _, statement) = binder;

        return new( "foreach",
            XsParsers.Bounded(
                static ctx =>
                {
                    var (scope, _) = ctx;
                    scope.Push( FrameType.Child );
                },
                Between(
                    Terms.Char( '(' ),
                        Terms.Text( "var" )
                            .SkipAnd( Terms.Identifier() )
                            .AndSkip( Terms.Text( "in" ) )
                            .And( expression ),
                    Terms.Char( ')' )
                ).Then( static ( ctx, parts ) =>
                {
                    var (scope, _) = ctx;
                    var (elementIdentifer, collection) = parts;

                    var elementName = elementIdentifer.ToString()!;
                    var elementVariable = Variable( 
                        collection.Type.GetElementType(),
                        elementName );

                    scope.Variables.Add( elementName, elementVariable );

                    return (elementVariable, collection);
                } )
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
                    var ((element, collection), body) = parts;

                    var bodyBlock = Block( body );
                    return ExpressionExtensions.ForEach( collection, element, bodyBlock );
                } ),
                static ctx =>
                {
                    var (scope, _) = ctx;
                    scope.Pop();
                }
            ).Named( "foreach" )
        );
    }
}
