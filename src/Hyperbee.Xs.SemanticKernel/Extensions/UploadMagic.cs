// UploadMagic.cs
using Hyperbee.XS.SemanticKernel.Extensions;
using Hyperbee.XS.SemanticKernel.Helpers;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Formatting;

namespace Hyperbee.XS.SemanticKernel.Magic;

/// <summary>
/// Adds the #!upload directive to a <see cref="CompositeKernel"/>.
/// Keep all directive plumbing in one place.
/// </summary>
public static class UploadMagic
{
    /// <summary>
    /// Registers the #!upload directive and its handler.
    /// Call once during <c>OnLoadAsync</c>.
    /// </summary>
    public static void RegisterUploadDirective( CompositeKernel composite, IFileHandler fileHandler )
    {
        // ----- directive description ---------------------------------------
        var fileParam = new KernelDirectiveParameter( "filePath" )
        {
            Description = "Local .csv or .xlsx file path",
            Required = true,
            AllowImplicitName = true
        };

        var uploadDirective = new KernelActionDirective( "#!upload" )
        {
            Description = "Upload the specified file to Azure Blob Storage",
            Parameters = { fileParam }
        };

        // ----- bind directive -> UploadCommand -> handler ------------------
        composite.AddDirective<UploadCommand>(
            uploadDirective,
            ( cmd, ctx ) => HandleUploadAsync( cmd, ctx, fileHandler ) );
    }

    // shared async handler
    private static async Task HandleUploadAsync(
        UploadCommand cmd,
        KernelInvocationContext ctx,
        IFileHandler fileHandler )
    {
        var path = cmd.FilePath;

        if ( !File.Exists( path ) )
        {
            ctx.Fail( cmd, message: $"File not found: {path}" );
            return;
        }

        var ext = Path.GetExtension( path ).ToLowerInvariant();
        if ( ext is not ".csv" and not ".xlsx" )
        {
            ctx.Fail( cmd, message: "Only .csv and .xlsx files are allowed." );
            return;
        }

        try
        {
            KernelInvocationContextExtensions.Display( ctx, "about to upload", HtmlFormatter.MimeType );
            await fileHandler.UploadToAzureAsync( path );
            KernelInvocationContextExtensions.Display( ctx, $"Uploaded **{Path.GetFileName( path )}**", HtmlFormatter.MimeType );
        }
        catch ( Exception ex )
        {
            ctx.Fail( cmd, message: ex.Message );
        }
    }
}
//  private async Task HandleUploadAsync( UploadCommand cmd, KernelInvocationContext context )
//     {
//         var file = cmd.FilePath;
//         if ( string.IsNullOrWhiteSpace( file ) )
//         {
//             context.Fail( cmd, message: "No file specified. Usage: #!upload \"path/to/file\"" );
//             return;
//         }

//         if ( !File.Exists( file ) )
//         {
//             context.Fail( cmd, message: $"File not found: {file}" );
//             return;
//         }

//         // Check for allowed file types (csv or xlsx)
//         var ext = Path.GetExtension( file ).ToLowerInvariant();
//         if ( ext != ".csv" && ext != ".xlsx" )
//         {
//             context.Fail( cmd, message: "Only .csv and .xlsx files are supported for upload." );
//             return;
//         }

//         if ( _fileHandler == null )
//         {
//             context.Fail( cmd, message: "IFileHandler service is not available." );
//             return;
//         }

//         try
//         {
//             await _fileHandler.UploadToAzureAsync( file );
//             KernelInvocationContextExtensions.Display( context, $"âœ“ Uploaded **{Path.GetFileName( file )}**", "text/markdown" );
//         }
//         catch ( Exception ex )
//         {
//             context.Fail( cmd, message: ex.Message );
//         }
//     }
// }

