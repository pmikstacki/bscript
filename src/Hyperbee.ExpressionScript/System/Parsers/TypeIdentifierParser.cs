using System.Linq.Expressions;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

internal class TypeIdentifierParser : Parser<Expression>
{
    private readonly TypeResolver _resolver;

    public TypeIdentifierParser( TypeResolver resolver )
    {
        _resolver = resolver;
    }

    public override bool Parse( ParseContext context, ref ParseResult<Expression> result )
    {
        context.EnterParser( this );

        var scanner = context.Scanner;
        var cursor = scanner.Cursor;

        var start = cursor.Position;
        var position = start;
        scanner.SkipWhiteSpaceOrNewLine();

        var stack = new Stack<(string Segment, TextPosition Position)>();

        while ( scanner.ReadIdentifier( out var segment ) )
        {
            stack.Push( (segment.ToString(), position) );
            position = cursor.Position;

            if ( !scanner.ReadChar( '.' ) )
                break;

            scanner.SkipWhiteSpaceOrNewLine();
        }

        while ( stack.Count > 0 )
        {
            var segments = stack.Select( loc => loc.Segment ).Reverse(); //TODO: Improve this
            var typeName = string.Join( ".", segments );
            var resolvedType = _resolver.ResolveType( typeName );

            if ( resolvedType != null )
            {
                result.Set( start.Offset, cursor.Position.Offset, Expression.Constant( resolvedType ) );
                context.ExitParser( this );
                return true;
            }

            cursor.ResetPosition( stack.Pop().Position );
        }

        cursor.ResetPosition( start );
        context.ExitParser( this );
        return false;
    }
}

internal static partial class XsParsers
{
    public static Parser<Expression> TypeIdentifier( TypeResolver resolver )
    {
        return new TypeIdentifierParser( resolver );
    }
}
