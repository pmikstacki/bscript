using System.Linq.Expressions;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Parsers;

internal class ValueIdentifierParser : Parser<Expression>
{
    private readonly ParseScope _scope;
    private readonly HashSet<string> _reservedKeywords;

    public ValueIdentifierParser( ParseScope scope, HashSet<string> reservedKeywords )
    {
        _scope = scope;
        _reservedKeywords = reservedKeywords;
    }

    public override bool Parse( ParseContext context, ref ParseResult<Expression> result )
    {
        var scanner = context.Scanner;
        var cursor = scanner.Cursor;

        var start = cursor.Position;
        scanner.SkipWhiteSpaceOrNewLine();

        if ( scanner.ReadIdentifier( out var identifier ) && !_reservedKeywords.Contains( identifier.ToString() ) )
        {

            if ( _scope.TryLookupVariable( identifier.ToString(), out var variable ) )
            {
                result.Set( start.Offset, cursor.Position.Offset, variable );
                return true;
            }
        }

        cursor.ResetPosition( start );
        return false;
    }
}

internal static partial class XsParsers
{
    public static Parser<Expression> ValueIdentifier( ParseScope scope, HashSet<string> reservedKeywords )
    {
        return new ValueIdentifierParser( scope, reservedKeywords );
    }
}

