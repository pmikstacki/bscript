using Parlot.Fluent;

using static Parlot.Fluent.Parsers;
namespace Hyperbee.XS.System.Parsers;

public static partial class XsParsers
{
    public static Parser<T> BoundedIf<T>(
        Func<ParseContext, bool> condition,
        Action<ParseContext> before,
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return If(
            condition,
            Bounded( before, parser, after )
        ).Named( "bounded-if" );
    }

    public static Parser<T> Bounded<T>(
        Action<ParseContext> before,
        Parser<T> parser,
        Action<ParseContext> after )
    {
        return Between(
            Always().Then<T>( ( ctx, _ ) =>
            {
                before?.Invoke( ctx );
                return default;
            } ),
            parser,
            Always().Then<T>( ( ctx, _ ) =>
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
