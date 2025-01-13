using System.Linq.Expressions;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Hyperbee.XS.System.Parsers.XsParsers;
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
                        .Or( Terms.Text( "%=" ) )
                        .Or( Terms.Text( "^=" ) )
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
                        "%=" => ModuloAssign( left, right ),
                        "^=" => SafePowerAssign( left, right ),
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
        var objectConstructor =
            Between(
                Terms.Char( '(' ),
                ArgsParser( expression ),
                Terms.Char( ')' )
            ).Then( static parts =>
                (ConstructorType.Object, parts, (IReadOnlyList<Expression>) null)
            );

        var arrayConstructor =
            Between(
                Terms.Char( '[' ),
                ZeroOrOne( Separated(
                    Terms.Char( ',' ),
                    expression
                ) ),
                Terms.Char( ']' )
            )
            .And(
                ZeroOrOne(
                    Between(
                        Terms.Char( '{' ),
                        Separated(
                            Terms.Char( ',' ),
                            expression
                        ),
                        Terms.Char( '}' )
                    )
                )
            )
            .Then( static parts =>
            {
                var (bounds, initial) = parts;

                return initial == null
                    ? (ConstructorType.ArrayBounds, bounds, null)
                    : (ConstructorType.ArrayInit, bounds, initial);
            } );


        return Terms.Text( "new" )
            .SkipAnd( TypeRuntime() )
            .And( OneOf( objectConstructor, arrayConstructor ) )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (type, (constructorType, arguments, initial)) = parts;

                switch ( constructorType )
                {
                    case ConstructorType.ArrayBounds:
                        if ( arguments.Count == 0 )
                            throw new InvalidOperationException( "Array bounds initializer requires at least one argument." );

                        return NewArrayBounds( type, arguments );

                    case ConstructorType.ArrayInit:
                        var arrayType = initial[^1].Type;

                        if ( type != arrayType && arrayType.IsArray && type != arrayType.GetElementType() )
                            throw new InvalidOperationException( $"Array of type {type.Name} does not match type {arrayType.Name}." );

                        return NewArrayInit( arrayType, initial );

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

