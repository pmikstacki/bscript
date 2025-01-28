namespace Hyperbee.XS;

public class XsDebugInfo
{
    internal string Source { get; set; }
    public List<BreakPoint> BreakPoints { get; set; } = [];
    public DebuggerCallback Debugger { get; set; }

    internal void InvokeDebugger( int line, int column, Dictionary<string, object> variables, string message )
    {
        if ( BreakPoints.Any( bp => bp.Line == line && (bp.Columns == null || bp.Columns.IsBetween( column )) ) )
        {
            Debugger?.Invoke( line, column, variables, message );
        }
    }


    public record BreakPoint( int Line, ColumnRange Columns = null );

    public delegate void DebuggerCallback( int line, int column, Dictionary<string, object> variables, string message );

    public record ColumnRange( int Start, int End )
    {
        internal bool IsBetween( int column ) => column >= Start && column <= End;
    }
}

