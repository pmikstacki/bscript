using System.Linq.Expressions;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;

using static System.Linq.Expressions.Expression;
using static Hyperbee.XS.System.Parsers.XsParsers;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    private static Parser<Expression> AssignmentParser( Parser<Expression> variable, Parser<Expression> expression )
    {
        return variable
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
                    var (left, op, right) = parts;

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
            ).Named( "assignment" );
    }
}
