namespace Hyperbee.XS;

public class XsDebugInfo
{
    internal string Source { get; set; }
    public List<Breakpoint> Breakpoints { get; set; }
    public DebuggerCallback Debugger { get; set; }

    internal void InvokeDebugger( int line, int column, Dictionary<string, object> variables, string message )
    {
        if ( Breakpoints == null )
        {
            Debugger?.Invoke( line, column, variables, message );
            return;
        }

        if ( Breakpoints.Any( bp => bp.Line == line && (bp.Columns == null || bp.Columns.Contain( column )) ) )
        {
            Debugger?.Invoke( line, column, variables, message );
        }
    }

    public record Breakpoint( int Line, ColumnRange Columns = null );

    public record ColumnRange( int Start, int End )
    {
        internal bool Contain( int column ) => column >= Start && column <= End;
    }

    public delegate void DebuggerCallback( int line, int column, Dictionary<string, object> variables, string message );

}

