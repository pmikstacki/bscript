using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Formatting;

namespace Hyperbee.XS.SemanticKernel.Extensions;

public static class AskMagic
{
    public static void RegisterAskDirective( CompositeKernel composite, SKProxy skProxy )
    {

        // ----- directive description ---------------------------------------
        var questionParam = new KernelDirectiveParameter( "question" )
        {
            Description = "Question to ask the Semantic Kernel",
            Required = true,
            AllowImplicitName = true
        };

        var askDirective = new KernelActionDirective( "#!ask" )
        {
            Description = "Ask a question to Azure OpenAI via Semantic Kernel with Azure AI Search integration always enabled",
            Parameters = { questionParam }
        };


        // Bind directive -> AskCommand -> handler
        composite.AddDirective<AskCommand>(
            askDirective,
            ( cmd, ctx ) => HandleAskAsync( cmd, ctx, skProxy ) );
    }

    private static async Task HandleAskAsync(
        AskCommand cmd,
        KernelInvocationContext ctx,
        SKProxy skProxy )
    {
        var question = cmd.Question;
        if ( string.IsNullOrWhiteSpace( question ) )
        {
            ctx.Fail( cmd, message: "Question cannot be empty" );
            return;
        }

        try
        {
            // Display a "thinking" message
            var thinkingMessage = new HtmlString( "Processing your question..." );

            KernelInvocationContextExtensions.Display( ctx, thinkingMessage.ToString(), HtmlFormatter.MimeType );

            // Send the question to the Semantic Kernel with notebook context (search is always enabled, max results always used)
            var response = await skProxy.AskAsync(
                question,
                includeCellContext: true );


            if ( string.IsNullOrWhiteSpace( response ) )
            {
                ctx.Fail( cmd, message: "No response received from Semantic Kernel" );
                return;
            }
            // Format the response as HTML
            var formattedResponse = new HtmlString( $"Answer:{response}" );

            KernelInvocationContextExtensions.Display( ctx, formattedResponse.ToString(), HtmlFormatter.MimeType );
        }
        catch ( Exception ex )
        {
            ctx.Fail( cmd, message: ex.Message );
        }
    }
}
