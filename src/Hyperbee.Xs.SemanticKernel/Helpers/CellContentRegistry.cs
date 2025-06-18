using System.Collections.Concurrent;
using System.Text;

namespace Hyperbee.XS.SemanticKernel.Helpers;

/// <summary>
/// Tracks the content of notebook cells for reference by the Semantic Kernel
/// </summary>
public class CellContentRegistry
{
    private readonly ConcurrentDictionary<string, CellInfo> _cellContents = new();

    public void AddOrUpdateCell( string cellId, string kernelName, string content )
    {
        _cellContents[cellId] = new CellInfo( kernelName, content );
    }

    public IReadOnlyDictionary<string, CellInfo> GetCells() => _cellContents;

    public string GetCellSummary()
    {
        if ( _cellContents.Count == 0 )
            return "No cells available";

        var summary = new StringBuilder();
        var index = 1;

        foreach ( var cell in _cellContents )
        {
            summary.AppendLine( $"Cell {index} ({cell.Value.KernelName}):" );
            summary.AppendLine( cell.Value.Content );
            summary.AppendLine();
            index++;
        }

        return summary.ToString();
    }

    public record CellInfo( string KernelName, string Content );
}
