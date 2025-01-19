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
                before?.Invoke( ctx );
                return default;
            } ),
            parser,
            Always().Then<Expression>( ( ctx, _ ) =>
            {
                after?.Invoke( ctx );
                return default;
            } )
        ).Named( "bounded" );
    }

    public static Parser<T> Bounded<T>(
        Action<ParseContext> before,
        Parser<T> parser )
    {
        return Bounded( before, parser, null );
    }

    public static Parser<T> Bounded<T>(
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return Bounded( null, parser, after );
    }
}
