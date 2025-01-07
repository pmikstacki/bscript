using System.Linq.Expressions;
using Parlot.Fluent;

using static Parlot.Fluent.Parsers;
namespace Hyperbee.XS.System.Parsers;

public static partial class XsParsers
{
    public static Parser<T> Bounded<T>(
        Action<ParseContext> before,
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return Between(
            Always().Then<Expression>( ( ctx, _ ) =>
            {
                before( ctx );
                return default;
            } ),
            parser,
            Always().Then<Expression>( ( ctx, _ ) =>
            {
                after( ctx );
                return default;
            } )
        );
    }

    public static Parser<T> Bounded<T>(
        Action<ParseContext> before,
        Parser<T> parser )
    {
        return Bounded( before, parser, _ => { } );
    }

    public static Parser<T> Bounded<T>(
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return Bounded( _ => { }, parser, after );
    }

    public static Parser<T> Bounded<T>(
        Parser<T> parser )
    {
        return Bounded( _ => { }, parser, _ => { } );
    }
}
