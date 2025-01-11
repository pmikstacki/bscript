using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

public static partial class XsParsers
{
    public static Parser<T> AndMaybe<T, U>( this Parser<T> parser, Func<T, Parser<U>> maybeFactory )
    {
        return parser.Then( ( ctx, parserResult ) =>
        {
            var maybeParser = maybeFactory( parserResult );
            var maybeResult = new ParseResult<U>();

            if ( maybeParser.Parse( ctx, ref maybeResult ) )
            {
                return (maybeResult.Value is T value ? value : default) ?? parserResult;
            }

            return parserResult;
        } );
    }

    public static Parser<T> AndMaybe<T, U>( this Parser<T> parser, Func<ParseContext, T, Parser<U>> maybeFactory )
    {
        return parser.Then( ( ctx, parserResult ) =>
        {
            var maybeParser = maybeFactory( ctx, parserResult );
            var maybeResult = new ParseResult<U>();

            if ( maybeParser.Parse( ctx, ref maybeResult ) )
            {
                return (maybeResult.Value is T value ? value : default) ?? parserResult;
            }

            return parserResult;
        } );
    }
}


