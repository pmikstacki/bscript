using System.Linq.Expressions;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

internal class TerminatedParser : Parser<Expression>
{
    private Parser<Expression> _parser;

    public TerminatedParser( Parser<Expression> parser )
    {
        _parser = parser;
        Name = $"{parser.Name} (Terminated)";
    }

    public override bool Parse( ParseContext context, ref ParseResult<Expression> result )
    {
        var prevResult = new ParseResult<Expression>();
        if ( !_parser.Parse( context, ref prevResult ) )
        {
            return false;
        }

        context.EnterParser( this );

        var scanner = context.Scanner;

        Peek( scanner, ';' );

        result.Set( prevResult.Start, scanner.Cursor.Position.Offset, prevResult.Value );

        context.ExitParser( this );
        return true;

        static void Peek( Scanner scanner, char c )
        {
            var start = scanner.Cursor.Position;

            scanner.SkipWhiteSpace();
            if ( scanner.ReadChar( c ) )
            {
                return;
            }

            scanner.Cursor.ResetPosition( start );
        }
    }
}

public static partial class XsParsers
{
    public static Parser<Expression> Terminated( this Parser<Expression> parser )
    {
        return new TerminatedParser( parser );
    }
}
