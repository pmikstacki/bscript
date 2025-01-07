using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    // Value Parsers

    private static Parser<Expression> AssignmentParser( Parser<Expression> expression )
    {
        return Terms.Identifier()
            .And(
                SkipWhiteSpace(
                    Terms.Text( "=" )
                        .Or( Terms.Text( "+=" ) )
                        .Or( Terms.Text( "-=" ) )
                        .Or( Terms.Text( "*=" ) )
                        .Or( Terms.Text( "/=" ) )
                        .Or( Terms.Text( "??=" ) )
                )
            )
            .And( expression )
            .Then<Expression>( static ( ctx, parts ) =>
                {
                    var (scope, _) = ctx;
                    var (ident, op, right) = parts;

                    var left = scope.LookupVariable( ident );

                    return op switch
                    {
                        "=" => Assign( left, right ),
                        "+=" => AddAssign( left, right ),
                        "-=" => SubtractAssign( left, right ),
                        "*=" => MultiplyAssign( left, right ),
                        "/=" => DivideAssign( left, right ),
                        "??=" => Assign( left, Coalesce( left, right ) ),
                        _ => throw new InvalidOperationException( $"Unsupported operator: {op}." )
                    };
                }
            );
    }

    private static Parser<Expression> DeclarationParser( Parser<Expression> expression )
    {
        return Terms.Text( "var" )
            .SkipAnd( Terms.Identifier() )
            .AndSkip( Terms.Char( '=' ) )
            .And( expression )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                var (ident, right) = parts;

                var left = ident.ToString()!;

                var variable = Variable( right.Type, left );
                scope.Variables.Add( left, variable );

                return Assign( variable, right );
            }
        );
    }

    private static Parser<Expression> NewParser( Parser<Expression> expression )
    {
        // TODO: Add optional array initializer

        return Terms.Text( "new" )
            .SkipAnd( XsParsers.TypeRuntime() )
            .And(
                OneOf(
                    Between(
                        Terms.Char( '(' ),
                        ArgumentsParser( expression ),
                        Terms.Char( ')' )
                    ).Then( static parts => (ConstructorType.Object, parts) ),
                    Between(
                        Terms.Char( '[' ),
                        Separated(
                            Terms.Char( ',' ),
                            expression
                        ),
                        Terms.Char( ']' )
                    )
                    //.And( arrayInitializer ) // TODO: Toggle between bounds and init if exists
                    .Then( static parts => (ConstructorType.ArrayBounds, parts) )
                )
            )
            .Then<Expression>( static parts =>
            {
                var (type, (constructorType, arguments)) = parts;  // TODO: Add initializer

                switch ( constructorType )
                {
                    case ConstructorType.ArrayBounds:
                        if ( arguments.Count == 0 )
                            throw new InvalidOperationException( "Array bounds initializer requires at least one argument." );

                        return NewArrayBounds( type, arguments );

                    case ConstructorType.ArrayInit:
                        throw new NotImplementedException( "Array initializer not implemented." );

                    //return NewArrayInit( type, arguments );

                    case ConstructorType.Object:
                        var constructor = type.GetConstructor( arguments.Select( arg => arg.Type ).ToArray() );

                        if ( constructor == null )
                            throw new InvalidOperationException( $"No matching constructor found for type {type.Name}." );

                        return New( constructor, arguments );

                    default:
                        throw new InvalidOperationException( $"Unsupported constructor type: {constructorType}." );
                }
            }
        );
    }

    private enum ConstructorType
    {
        Object,
        ArrayBounds,
        ArrayInit,
    }
}

