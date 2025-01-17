using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

public record KeyParserPair<T>( string Key, Parser<T> Parser );

public sealed class LookupParser<T> : Parser<T>
{
    private readonly Dictionary<string, Parser<T>> _parsers = new();

    public LookupParser<T> Add( string keyword, Parser<T> parser )
    {
        if ( string.IsNullOrWhiteSpace( keyword ) )
        {
            throw new ArgumentException( "Keyword cannot be null or whitespace.", nameof( keyword ) );
        }

        ArgumentNullException.ThrowIfNull( parser );

        _parsers[keyword] = parser;
        return this;
    }

    public LookupParser<T> Add( params KeyParserPair<T>[] parsers )
    {
        ArgumentNullException.ThrowIfNull( parsers );

        foreach ( var (keyword, parser) in parsers )
        {
            Add( keyword, parser );
        }
        return this;
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var start = scanner.Cursor.Position;

        scanner.SkipWhiteSpaceOrNewLine();

        if ( scanner.ReadIdentifier( out var identifier ) && _parsers.TryGetValue( identifier.ToString(), out var parser ) )
        {
            if ( parser.Parse( context, ref result ) )
            {
                scanner.SkipWhiteSpaceOrNewLine();
                context.ExitParser( this );
                return true;
            }
        }

        scanner.Cursor.ResetPosition( start );
        context.ExitParser( this );
        return false;
    }
}

public static partial class XsParsers
{
    public static LookupParser<T> IdentifierLookup<T>( string name = "" )
    {
        return new LookupParser<T>() { Name = name };
    }
}
