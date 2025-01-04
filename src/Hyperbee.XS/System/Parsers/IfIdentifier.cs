using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

public sealed class IfIdentifier<T> : Parser<T>
{
    private readonly string _match;
    private readonly Parser<T> _parser;

    public IfIdentifier( ReadOnlySpan<char> match, Parser<T> parser )
    {
        _parser = parser ?? throw new ArgumentNullException( nameof(parser) );
        _match = match.ToString();

        Name = $"{parser.Name} (IfIdentifier)";
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var valid = false;

        var start = context.Scanner.Cursor.Position;
        scanner.SkipWhiteSpaceOrNewLine(); 

        if ( scanner.ReadIdentifier( out var identifier ) && identifier.SequenceEqual( _match ) )
        {
            if ( _parser.Parse( context, ref result ) )
            {
                scanner.SkipWhiteSpaceOrNewLine();
                valid = true;
            }
        }
        
        if ( !valid )
            context.Scanner.Cursor.ResetPosition( start );

        context.ExitParser( this );
        return valid;
    }
}

internal static partial class XsParsers
{
    public static Parser<T> IfIdentifier<T>( ReadOnlySpan<char> match, Parser<T> parser )
    {
        return new IfIdentifier<T>( match, parser );
    }
}

