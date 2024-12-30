using System.Linq.Expressions;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Parsers;

internal class ValueIdentifierParser : Parser<Expression>
{
    private readonly ParseScope _scope;

    public ValueIdentifierParser( ParseScope scope )
    {
        _scope = scope;
    }

    public override bool Parse( ParseContext context, ref ParseResult<Expression> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;

        var start = cursor.Position;
        scanner.SkipWhiteSpaceOrNewLine();

        if ( scanner.ReadIdentifier( out var identifier ) )
        {
            if ( _scope.TryLookupVariable( identifier.ToString(), out var variable ) )
            {
                result.Set( start.Offset, cursor.Position.Offset, variable );
                context.ExitParser( this );
                return true;
            }
        }

        cursor.ResetPosition( start );
        context.ExitParser( this );
        return false;
    }
}

internal static partial class XsParsers
{
    public static Parser<Expression> ValueIdentifier( ParseScope scope )
    {
        return new ValueIdentifierParser( scope );
    }
}

