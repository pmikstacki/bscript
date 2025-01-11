using System.Linq.Expressions;
using Hyperbee.XS.System;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{

    // Lambda Parsers

    private static Parser<Expression> LambdaParser( Parser<Expression> identifier, Parser<Expression> primaryExpression, Deferred<Expression> statement )
    {
        return Between(
                Terms.Char( '(' ),
                Parameters( identifier ),
                Terms.Char( ')' )
            )
            .AndSkip( Terms.Text( "=>" ) )
            .And(
                OneOf(
                    primaryExpression,
                    Between(
                            Terms.Char( '{' ),
                            ZeroOrMany( statement ),
                            Terms.Char( '}' )
                        )
                        .Then<Expression>( static ( ctx, body ) =>
                        {
                            var (scope, _) = ctx;
                            var returnLabel = scope.Frame.ReturnLabel;

                            if ( returnLabel != null )
                            {
                                return Block(
                                    body.Concat(
                                        [Label( returnLabel, Default( returnLabel.Type ) )]
                                    )
                                );
                            }

                            return Block( body );
                        } )
                )
            )
            .Then<Expression>( static ( ctx, parts ) =>
                {
                    var (scope, _) = ctx;
                    var (parameters, body) = parts;

                    try
                    {
                        return Lambda( body, parameters );
                    }
                    finally
                    {
                        scope.Pop();
                    }
                }
            );

        static Parser<ParameterExpression[]> Parameters( Parser<Expression> identifier )
        {
            return ZeroOrOne(
                    Separated(
                        Terms.Char( ',' ),
                        identifier.And( Terms.Identifier() )
                    )
                )
                .Then( static ( ctx, parts ) =>
                {
                    var (scope, resolver) = ctx;

                    scope.Push( FrameType.Parent );

                    if ( parts == null )
                        return [];

                    return parts.Select( p =>
                    {
                        var (typeName, paramName) = p;

                        var type = resolver.ResolveType( typeName.ToString() )
                                   ?? throw new InvalidOperationException( $"Unknown type: {typeName}." );

                        var name = paramName.ToString()!;
                        var parameter = Parameter( type, name );

                        scope.Variables.Add( name, parameter );

                        return parameter;

                    } ).ToArray();
                } );
        }
    }
    //Expression baseExpression, Parser<Expression> expression 

    private static Parser<Expression> LambdaInvokeParser( Expression targetExpression, Parser<Expression> expression )
    {
        return Between(
                Terms.Char( '(' ),
                ArgumentsParser( expression ),
                Terms.Char( ')' )
            )
            .Then<Expression>( args => Invoke( targetExpression, args ) 
        );
    }

    //private static Parser<Expression> LambdaInvokeParser( Parser<Expression> primaryExpression )
    //{
    //    return Terms.Identifier()
    //        .And(
    //            Between(
    //                Terms.Char( '(' ),
    //                ArgumentsParser( primaryExpression ),
    //                Terms.Char( ')' )
    //            )
    //        )
    //        .Then<Expression>( static ( ctx, parts ) =>
    //        {
    //            var (scope, _) = ctx;
    //            var (targetName, invocationArguments) = parts;

    //            var targetExpression = scope.LookupVariable( targetName );

    //            return Invoke(
    //                targetExpression,
    //                invocationArguments
    //            );
    //        } );
    //}
}

