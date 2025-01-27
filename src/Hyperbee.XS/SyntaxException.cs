using System.Data.Common;
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

    public override string Message => $"{base.Message} {Buffer.ShowPosition( Line, Column )}";

}


public static partial class PositionExtensions
{
    public static string GetLine( this string buffer, int line )
    {
        if ( buffer == null || line <= 0 )
            return string.Empty;

        var lines = SplitLinesRegex().Split( buffer );

        if ( line > lines.Length )
            return string.Empty;

        return lines[line - 1];
    }

    public static string ShowPosition( this string buffer, int line, int column )
    {
        if ( buffer == null || line <= 0 || column <= 0 )
            return string.Empty;

        var lines = SplitLinesRegex().Split( buffer );

        if ( line > lines.Length )
            return string.Empty; ;

        var lineText = lines[line - 1];
        var caretLine = new string( ' ', column - 1 ) + "^";

        // Create the formatted message
        return $"(Line: {line}, Column: {column})\n{lineText}\n{caretLine}";
    }

    [GeneratedRegex( @"\r\n|\n|\r" )]
    private static partial Regex SplitLinesRegex();
}
