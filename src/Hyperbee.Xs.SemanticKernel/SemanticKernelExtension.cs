using Hyperbee.XS.SemanticKernel.Extensions;
using Hyperbee.XS.SemanticKernel.Helpers;
using Hyperbee.XS.SemanticKernel.Magic;
using Microsoft.DotNet.Interactive;

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;


namespace Hyperbee.XS.SemanticKernel;

/// <summary>
/// Interactive extension: registers the UploadCommand and wires analysis middleware.
/// The directive is available in all kernels.
/// </summary>
public class SemanticKernelExtension : Microsoft.DotNet.Interactive.IKernelExtension
{
    private SKProxy? _skProxy = null;
    private IFileHandler? _fileHandler = null;


    public Task OnLoadAsync( Microsoft.DotNet.Interactive.Kernel kernel )
    {
        if ( kernel is not CompositeKernel composite )
            return Task.CompletedTask;

        _fileHandler = new FileHandler();
        _skProxy = new SKProxy();

        try
        {
            if ( _skProxy.InitializationError != null )
            {
                var ctx = KernelInvocationContext.Current;
                if ( ctx != null )
                {

                    KernelInvocationContextExtensions.Display( ctx, $"**Azure OpenAI Initialization Error:** {_skProxy.InitializationError}", HtmlFormatter.MimeType );
                }
                return Task.CompletedTask;
            }

            UploadMagic.RegisterUploadDirective( composite, _fileHandler );
            AskMagic.RegisterAskDirective( composite, _skProxy );

            // Register middleware using the helper class
            var middleware = new Helpers.SemanticKernelMiddleware( _skProxy );
            middleware.RegisterMiddleware( composite );

            return Task.CompletedTask;
        }
        catch ( KernelException ex )
        {
            // Display errors inline in the notebook
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"**Kernel Exception Error:** {ex.Message}", HtmlFormatter.MimeType );
            }
            return Task.CompletedTask;
        }
        catch ( Exception ex )
        {
            // Display errors inline in the notebook
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"**Exception Error:** {ex.Message}", HtmlFormatter.MimeType );
            }
            return Task.CompletedTask;

        }
    }
}
