using Azure.Storage.Blobs;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace Hyperbee.XS.SemanticKernel.Helpers;

public interface IFileHandler
{
    Task UploadToAzureAsync( string filePath );
}

public class FileHandler : IFileHandler
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public FileHandler( string? connectionString = null, string? containerName = null )
    {
        connectionString ??= AppSettingsHelper.Get( "AzureBlobStorage:ConnectionString" );
        containerName ??= AppSettingsHelper.Get( "AzureBlobStorage:ContainerName" );

        if ( string.IsNullOrWhiteSpace( connectionString ) || string.IsNullOrWhiteSpace( containerName ) )
        {
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"**File Configuration:** Storage account or container name not found", HtmlFormatter.MimeType );
            }
            throw new InvalidOperationException( "Azure Blob Storage connection string or container name is missing." );
        }


        _containerName = containerName;
        _blobServiceClient = new BlobServiceClient( connectionString );
    }

    public async Task UploadToAzureAsync( string filePath )
    {
        if ( !File.Exists( filePath ) )
        {
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"**FilePath:** File not found", HtmlFormatter.MimeType );
            }
            throw new FileNotFoundException( $"File not found: {filePath}", filePath );
        }

        var container = _blobServiceClient.GetBlobContainerClient( _containerName );
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlobClient( Path.GetFileName( filePath ) );
        await blob.UploadAsync( filePath, overwrite: true );
    }
}
