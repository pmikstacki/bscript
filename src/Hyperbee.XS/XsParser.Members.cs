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

    //private static Parser<Expression> IndexerAccessParser( Parser<Expression> baseExpression, Parser<Expression> expression )
    //{
    //    return baseExpression
    //    .And(
    //        Between(
    //            Terms.Char( '[' ),
    //            Separated(
    //                Terms.Char( ',' ),
    //                expression
    //            ),
    //            Terms.Char( ']' )
    //        ) )
    //    .Then<Expression>( static parts =>
    //    {
    //        var (target, indexes) = parts;

    //        var indexer = target.Type
    //            .GetProperties()
    //            .FirstOrDefault( p => p.GetIndexParameters()
    //                .Select( x => x.ParameterType )
    //                .SequenceEqual( indexes.Select( i => i.Type ) ) );

    //        if ( indexer == null )
    //            throw new InvalidOperationException( $"No indexer found on type '{target.Type}' with {indexes.Count} parameters." );

    //        return Property( target, indexer, [.. indexes] );
    //    } );

    //}

    private static Parser<Expression> IndexerAccessParser( Expression target, Parser<Expression> expression )
    {
        return //expression.And(
            Between(
                Terms.Char( '[' ),
                Separated( Terms.Char( ',' ), expression ),
                Terms.Char( ']' )
            )
        //)
        .Then<Expression>( indexes =>
        {
            //var (target, indexes) = parts;

            // Retrieve all indexers on the target's type
            var indexers = target.Type.GetProperties()
                .Where( p => p.GetIndexParameters().Length == indexes.Count )
                .ToArray();

            if ( indexers.Length == 0 )
            {
                throw new InvalidOperationException(
                    $"No indexers found on type '{target.Type}' with {indexes.Count} parameters." );
            }

            // Find the best match based on parameter types
            var indexer = indexers.FirstOrDefault( p =>
                p.GetIndexParameters()
                    .Select( param => param.ParameterType )
                    .SequenceEqual( indexes.Select( i => i.Type ) ) );

            if ( indexer == null )
            {
                throw new InvalidOperationException(
                    $"No matching indexer found on type '{target.Type}' with parameter types: " +
                    $"{string.Join( ", ", indexes.Select( i => i.Type.Name ) )}." );
            }

            // Generate the property access expression
            return Expression.Property( target, indexer, indexes.ToArray() );
        } );
    }


    private static Parser<Expression> MemberAccessParser( Expression baseExpression, Parser<Expression> expression )
    {
        return Terms.Char( '.' )
            .SkipAnd(
                Separated(
                    Terms.Char( '.' ),
                    Terms.Identifier()
                        .And(
                            ZeroOrOne(
                                Between(
                                    Terms.Char( '<' ),
                                    TypeArgsParser(),
                                    Terms.Char( '>' )
                                )
                            )
                        )
                        .And(
                            ZeroOrOne(
                                Between(
                                    Terms.Char( '(' ),
                                    ArgumentsParser( expression ),
                                    Terms.Char( ')' )
                                )
                            )
                        )
                )
            )
            .Then( accesses =>
            {
                var current = baseExpression;

                foreach ( var (memberName, typeArgs, args) in accesses )
                {
                    var type = ConvertToType( current );
                    var name = memberName.ToString()!;

                    if ( args != null )
                    {
                        // Resolve method call
                        var methodInfo = TypeResolver.FindMethod( type, name, typeArgs, args );

                        current = methodInfo?.IsStatic switch
                        {
                            true => Call( methodInfo, args.ToArray() ),
                            false => Call( current, methodInfo, args.ToArray() ),
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

