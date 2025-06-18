using Microsoft.DotNet.Interactive.Commands;

namespace Hyperbee.XS.SemanticKernel.Extensions;

/// <summary>
/// Command to upload a single local file to Azure Blob Storage.
/// </summary>
public class UploadCommand : KernelCommand
{
    public UploadCommand( string filePath )
    {
        if ( string.IsNullOrWhiteSpace( filePath ) )
            throw new ArgumentException( "File path must not be empty", nameof( filePath ) );
        FilePath = filePath;
    }

    public string FilePath { get; }
}