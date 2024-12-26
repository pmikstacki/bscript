using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.Parsers;

internal class WhitespaceOrNewLineOrCommentParser : Parser<TextSpan>
{
    public override bool Parse( ParseContext context, ref ParseResult<TextSpan> result )
    {
        var scanner = context.Scanner;
        var cursor = scanner.Cursor;
        
        while ( true )
        {
            if ( scanner.SkipWhiteSpaceOrNewLine() )
            {
                continue;
            }

            // Check for trailing comments

            if ( !cursor.Match( '/' ) || cursor.PeekNext() != '/' )
            {
                return false;
            }

            cursor.Advance( 2 );
            while ( !cursor.Eof && !Character.IsNewLine( cursor.Current ) )
            {
                cursor.Advance();
            }
        }
    }
}

internal static partial class XsParsers
{
    public static Parser<TextSpan> WhitespaceOrNewLineOrComment()
    {
        return new WhitespaceOrNewLineOrCommentParser();
    }
}

