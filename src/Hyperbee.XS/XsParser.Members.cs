using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.System;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    // Member Parsers

    private static Parser<Expression> IndexerAccessParser( Parser<Expression> baseExpression, Parser<Expression> expression )
    {
        return baseExpression
        .And(
            Between(
                Terms.Char( '[' ),
                Separated(
                    Terms.Char( ',' ),
                    expression
                ),
                Terms.Char( ']' )
            ) )
        .Then<Expression>( static parts =>
        {
            var (target, indexes) = parts;

            var indexer = target.Type
                .GetProperties()
                .FirstOrDefault( p => p.GetIndexParameters()
                    .Select( x => x.ParameterType )
                    .SequenceEqual( indexes.Select( i => i.Type ) ) );

            if ( indexer == null )
                throw new InvalidOperationException( $"No indexer found on type '{target.Type}' with {indexes.Count} parameters." );

            return Property( target, indexer, [.. indexes] );
        } );

    }

    private static Parser<Expression> MemberAccessParser( Parser<Expression> baseExpression, Parser<Expression> expression )
    {
        return baseExpression
            .AndSkip( Terms.Char( '.' ) )
            .And(
                Separated(
                    Terms.Char( '.' ),
                    Terms.Identifier().And(
                        ZeroOrOne(
                            Between(
                                Terms.Char( '(' ),
                                Arguments( expression ),
                                Terms.Char( ')' )
                            )
                        )
                    )
                )
            )
            .Then( static parts =>
            {
                var (current, accesses) = parts;

                foreach ( var (memberName, arguments) in accesses )
                {
                    var type = current switch
                    {
                        ConstantExpression ce => ce.Value as Type ?? ce.Type,
                        Expression e => e.Type,
                        _ => throw new InvalidOperationException( "Invalid target expression." )
                    };

                    var name = memberName.ToString()!;

                    if ( arguments != null )
                    {
                        // Resolve method call
                        var methodInfo = TypeResolver.FindMethod( type, name, arguments );

                        current = methodInfo?.IsStatic switch
                        {
                            true => Call( methodInfo, arguments.ToArray() ),
                            false => Call( current, methodInfo, arguments.ToArray() ),
                            null => throw new InvalidOperationException( $"Method '{name}' not found on type '{type}'." )
                        };
                    }
                    else
                    {
                        // Resolve property or field
                        const BindingFlags BindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                        var member = current.Type.GetMember( name, BindingAttr ).FirstOrDefault();

                        current = member?.MemberType switch
                        {
                            MemberTypes.Property => Property( current, (PropertyInfo) member ),
                            MemberTypes.Field => Field( current, (FieldInfo) member ),
                            null => throw new InvalidOperationException( $"Member '{name}' not found on type '{current.Type}'." ),
                            _ => throw new InvalidOperationException( $"Unsupported member type: {member.MemberType}." )
                        };
                    }
                }

                return current;
            } );
    }
}

