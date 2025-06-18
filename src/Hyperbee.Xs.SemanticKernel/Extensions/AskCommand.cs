using Microsoft.DotNet.Interactive.Commands;

namespace Hyperbee.XS.SemanticKernel.Extensions;

public class AskCommand : KernelCommand
{
    public AskCommand( string question )
    {
        if ( string.IsNullOrWhiteSpace( question ) )
            throw new ArgumentException( "Question must not be empty", nameof( question ) );
        Question = question;
    }

    public string Question { get; }
}
