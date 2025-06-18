using Hyperbee.XS.SemanticKernel.Helpers;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Hyperbee.XS.SemanticKernel;

/// <summary>
/// Wrapper for Semantic Kernel with Azure OpenAI Chat Completion,
/// exposing SKProxy via .NET Interactive extension, matching official docs.
/// </summary>
public class SKProxy
{
    public Microsoft.SemanticKernel.Kernel? _kernel;
    public string? _initializationError;
    public string? InitializationError => _initializationError;
    public bool IsReady => string.IsNullOrEmpty( _initializationError ) && _kernel != null;

    public IChatCompletionService? ChatCompletionService { get; private set; }
    public readonly Helpers.ChatHistory _chatHistory = new();

    public readonly CellContentRegistry _cellRegistry = new();

    public readonly SearchHandler _searchHandler = new();


    public SKProxy()
    {
        try
        {
            // Retrieve required configuration values for Azure OpenAI only
            var deploymentName = AppSettingsHelper.Get( "AzureOpenAI:DeploymentName" );
            var apiKey = AppSettingsHelper.Get( "AzureOpenAI:ApiKey" );
            var endpoint = AppSettingsHelper.Get( "AzureOpenAI:Endpoint" );

            var ctx = KernelInvocationContext.Current;

            var missing = new List<string>();
            if ( string.IsNullOrWhiteSpace( deploymentName ) ) missing.Add( "DeploymentName" );
            if ( string.IsNullOrWhiteSpace( apiKey ) ) missing.Add( "ApiKey" );
            if ( string.IsNullOrWhiteSpace( endpoint ) ) missing.Add( "Endpoint" );

            if ( missing.Count > 0 )
            {
                _initializationError = $"Missing Azure OpenAI config: {string.Join( ", ", missing )}. Set these in appsettings.json.";
                if ( ctx != null )
                {
                    KernelInvocationContextExtensions.Display( ctx, $"Azure OpenAI config missing: {string.Join( ", ", missing )}. Set in appsettings.json.", HtmlFormatter.MimeType );
                }
                return;
            }

            // Create the kernel builder for Azure OpenAI only
            var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                apiKey: apiKey,
                endpoint: endpoint );

            _kernel = builder.Build();
            ChatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }
        catch ( Exception ex )
        {
            _initializationError = $"Azure OpenAI init failed: {ex.Message}";
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"Azure OpenAI init failed: {ex.Message}", HtmlFormatter.MimeType );
            }
        }
    }

    /// Send a prompt to the chat completion service.
    /// </summary>
    /// <param name="prompt">The question to ask</param>
    /// <param name="includeCellContext">Whether to include notebook cell context</param>
    public async Task<string> AskAsync( string prompt, bool includeCellContext = false )
    {
        if ( !string.IsNullOrEmpty( _initializationError ) )
            return $"Cannot process request: {_initializationError}";

        if ( _kernel == null )
            return "Semantic Kernel is not initialized.";

        if ( ChatCompletionService == null )
            return "ChatCompletionService is not available.";

        try
        {
            // Build the full prompt with notebook context if requested
            string fullPrompt = prompt;
            if ( includeCellContext )
            {
                var cellSummary = _cellRegistry.GetCellSummary();
                fullPrompt = $"{prompt}\n\nNotebook Context:\n{cellSummary}";
            }

            // Always retrieve relevant information from Azure AI Search
            string searchContext = "";
            if ( !string.IsNullOrEmpty( prompt ) && _searchHandler != null )
            {
                KernelInvocationContextExtensions.Display( KernelInvocationContext.Current, "Searching knowledge base...", HtmlFormatter.MimeType );
                searchContext = await _searchHandler.SearchAsync( prompt, 5 ); // Always use max result count 5
            }

            // Add search results to the prompt if available, and always include both if requested
            if ( !string.IsNullOrEmpty( searchContext ) )
            {
                fullPrompt = $"{prompt}\n\nRelevant information:\n{searchContext}";
                if ( includeCellContext )
                {
                    var cellSummary = _cellRegistry.GetCellSummary();
                    fullPrompt += $"\n\nNotebook Context:\n{cellSummary}";
                }
            }

            // Add the new user message to the chat history
            _chatHistory.AddUserMessage( fullPrompt );
            // Get chat history in the format expected by the chat completion service
            var chatHistory = _chatHistory.GetUserMessages();

            var result = await ChatCompletionService.GetChatMessageContentAsync( chatHistory, kernel: _kernel );

            if ( result != null )
                _chatHistory.AddAssistantMessage( result.ToString() );

            return result?.ToString() ?? string.Empty;
        }
        catch ( KernelException ex )
        {
            var fullMessage = $"Request error: {ex.Message}";
            KernelInvocationContextExtensions.Display( KernelInvocationContext.Current, fullMessage, HtmlFormatter.MimeType );
            return fullMessage;
        }
        catch ( Exception ex )
        {
            var errorMessage = $"Request error: {ex.Message}";
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"AskAsync error: {ex.Message}", HtmlFormatter.MimeType );
            }
            return errorMessage;
        }
    }
}

