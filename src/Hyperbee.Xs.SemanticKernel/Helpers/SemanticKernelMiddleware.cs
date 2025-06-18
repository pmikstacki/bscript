using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Hyperbee.XS.SemanticKernel.Helpers;

/// <summary>
/// Middleware helper for Semantic Kernel integration with interactive notebooks
/// </summary>
public class SemanticKernelMiddleware
{
    private readonly SKProxy _skProxy;
    private static int _extensionLoadedMessageShown = 0;


    public SemanticKernelMiddleware( SKProxy skProxy )
    {
        _skProxy = skProxy ?? throw new ArgumentNullException( nameof( skProxy ) );
    }

    /// <summary>
    /// Registers middleware with the composite kernel to track cell execution and display welcome message
    /// </summary>

    public void RegisterMiddleware( CompositeKernel composite )
    {
        composite.AddMiddleware( async ( command, context, next ) =>
        {
            // Show extension loaded message only once, on first cell execution
            if ( Interlocked.CompareExchange( ref _extensionLoadedMessageShown, 1, 0 ) == 0 )
            {
                var message = new HtmlString(
                """
                <details>
                    <summary>Chat Support</summary>
                    <p>The notebook supports chat with AI.</p>
                    <p>Use <code>#!upload</code> to upload a file to azure storage</p>
                    <p>Use <code>#!ask</code> to ask a question to the Semantic Kernel</p>
                </details>
                """ );
                var formattedValue = new FormattedValue(
                    HtmlFormatter.MimeType,
                    message.ToDisplayString( HtmlFormatter.MimeType )
                );
                await composite.SendAsync( new DisplayValue( formattedValue, Guid.NewGuid().ToString() ) );
            }

            // Track cell execution for context
            if ( command is SubmitCode submitCode &&
                !string.IsNullOrWhiteSpace( submitCode.Code ) &&
                _skProxy != null &&
                context.HandlingKernel != null )
            {
                // Generate a unique identifier for the cell
                _skProxy._cellRegistry.AddOrUpdateCell(
                    command.GetHashCode().ToString(),
                    context.HandlingKernel.Name,
                    submitCode.Code );
            }

            await next( command, context );
        } );

    }
}
