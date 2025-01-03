using Parlot;

namespace Hyperbee.XS;

public class SyntaxErrorException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public int Offset { get; }

    public string Buffer { get; }

    public ReadOnlySpan<char> Span => Buffer != null ? Buffer.AsSpan( Offset ) : ReadOnlySpan<char>.Empty;

    public SyntaxErrorException( string message, Cursor cursor = null )
        : base( message )
    {
        if ( cursor == null )
            return;

        Line = cursor.Position.Line;
        Column = cursor.Position.Column;
        Offset = cursor.Position.Offset;
        Buffer = cursor.Buffer;
    }

    public override string ToString() => $"({Line}:{Column} {Offset}) - {Message}";
}
