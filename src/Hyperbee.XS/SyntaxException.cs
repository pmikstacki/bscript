using System.Text.RegularExpressions;
using Parlot;

namespace Hyperbee.XS;

public partial class SyntaxException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public int Offset { get; }

    public string Buffer { get; }

    public ReadOnlySpan<char> Span => Buffer != null ? Buffer.AsSpan( Offset ) : ReadOnlySpan<char>.Empty;

    public SyntaxException( string message, Cursor cursor = null )
        : base( message )
    {
        if ( cursor == null )
            return;

        Line = cursor.Position.Line;
        Column = cursor.Position.Column;
        Offset = cursor.Position.Offset;
        Buffer = cursor.Buffer;
    }

    public override string Message
    {
        get
        {
            if ( Buffer == null || Line <= 0 || Column <= 0 )
                return base.Message;

            var lines = SplitLinesRegex().Split( Buffer );

            if ( Line > lines.Length )
                return base.Message;

            var errorLine = lines[Line - 1];
            var caretLine = new string( ' ', Column - 1 ) + "^";

            // Create the formatted message
            return $"{base.Message} (Line: {Line}, Column: {Column})\n{errorLine}\n{caretLine}";
        }
    }

    [GeneratedRegex( @"\r\n|\n|\r" )]
    private static partial Regex SplitLinesRegex();
}
