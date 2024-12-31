using System.Linq.Expressions;
using Hyperbee.Collections;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

internal class ValueIdentifierParser : Parser<Expression>
{
    private readonly LinkedDictionary<string, ParameterExpression> _variables;

    public ValueIdentifierParser( LinkedDictionary<string, ParameterExpression> variables )
    {
        _variables = variables;
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
            if ( _variables.TryGetValue( identifier.ToString(), out var variable ) )
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
    public static Parser<Expression> ValueIdentifier( LinkedDictionary<string, ParameterExpression> variables )
    {
        return new ValueIdentifierParser( variables );
    }
}

