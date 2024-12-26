using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Parsers;

internal class ZeroOrManyUntilParser<T,U> : Parser<IReadOnlyList<T>>
{
    private readonly Parser<T> _parser;
    private readonly Parser<U> _untilParser;

    public ZeroOrManyUntilParser( Parser<T> parser, Parser<U> untilParser )
    {
        _parser = parser;
        _untilParser = untilParser;
    }

    public override bool Parse( ParseContext context, ref ParseResult<IReadOnlyList<T>> result )
    {
        context.EnterParser( this );

        var results = default(List<T>);
        var current = context.Scanner.Cursor.Position;

        var first = true;
        var start = 0;
        var end = 0;

        var untilCheck = new ParseResult<U>();
        var parsed = new ParseResult<T>();

        while ( !context.Scanner.Cursor.Eof )
        {
            if ( _untilParser.Parse( context, ref untilCheck ) )
            {
                context.Scanner.Cursor.ResetPosition( current );
                break;
            }

            // Parse the next statement

            if ( _parser.Parse( context, ref parsed ) )
            {
                if ( first )
                {
                    first = false;
                    start = parsed.Start;
                }

                end = parsed.End;

                results ??= [];
                results.Add( parsed.Value );

                current = context.Scanner.Cursor.Position; 
            }
            else
            {
                break;
            }
        }

        result.Set( start, end, results ?? (IReadOnlyList<T>) [] );
        context.ExitParser( this );
        return true;
    }
}

internal static partial class XsParsers
{
    public static Parser<IReadOnlyList<T>> ZeroOrManyUntil<T, U>( Parser<T> parser, Parser<U> untilParser )
    {
        return new ZeroOrManyUntilParser<T, U>( parser, untilParser );
    }
}

