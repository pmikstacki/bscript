using System.Reflection;
using System.Text;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.Configuration;

namespace Hyperbee.XS.SemanticKernel.Helpers;

/// <summary>
/// Handles integration with Azure AI Search for retrieval-augmented generation
/// </summary>
public class SearchHandler
{
    private SearchClient? _searchClient;

    public SearchHandler()
    {
        try
        {
            var endpoint = AppSettingsHelper.Get( "AzureAISearch:Endpoint" );
            var key = AppSettingsHelper.Get( "AzureAISearch:Key" );
            var indexName = AppSettingsHelper.Get( "AzureAISearch:IndexName" );

            if ( string.IsNullOrWhiteSpace( endpoint ) ||
                string.IsNullOrWhiteSpace( key ) ||
                string.IsNullOrWhiteSpace( indexName ) )
            {

                if ( string.IsNullOrWhiteSpace( endpoint ) || string.IsNullOrWhiteSpace( key ) || string.IsNullOrWhiteSpace( indexName ) )
                {
                    var ctx = KernelInvocationContext.Current;
                    if ( ctx != null )
                    {
                        KernelInvocationContextExtensions.Display( ctx, $"**Search Configuration:** Azure AI Search endpoint, key, or index name is missing.", HtmlFormatter.MimeType );
                    }
                    throw new InvalidOperationException( "Azure AI Search endpoint, key, or index name is missing." );
                }
                return;
            }

            // Create an Azure AI Search credential
            var credential = new AzureKeyCredential( key );

            // Create the search client
            _searchClient = new SearchClient( new Uri( endpoint ), indexName, credential );
        }
        catch ( Exception ex )
        {
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"**Search Configuration:** SearchHandler initialization error: {ex.Message}", HtmlFormatter.MimeType );
            }
            return;
        }
    }

    /// <summary>
    /// Searches the Azure AI Search index for relevant documents based on the user query
    /// </summary>
    /// <param name="query">The user's query</param>
    /// <param name="top">Maximum number of results to return</param>
    /// <returns>A formatted string containing relevant search results or an error message</returns>
    public async Task<string> SearchAsync( string query, int top = 3 )
    {

        try
        {
            // Create search options
            var options = new SearchOptions
            {
                Size = top,
                IncludeTotalCount = true,
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "Semantic-config"
                }
            };

            // Specify which fields to retrieve
            var fields = new[]
            {
                "PitcherID", "GameID", "PlayID", "PitchId", "PitchNum", "PitchNumberThisAtBat", "HitterID", "Outs", "BallsBeforePitch", "StrikesBeforePitch", "Strike", "Ball", "Foul", "Swinging", "Looking"
            };
            foreach ( var field in fields )
            {
                options.Select.Add( field );
            }

            // Execute the search
            if ( _searchClient == null )
            {
                return $"Cannot search: SearchClient is not initialized.";
            }
            var response = await _searchClient.SearchAsync<SearchDocument>( query, options );

            // Format the results
            return FormatSearchResults( response.Value, query );
        }
        catch ( Exception ex )
        {
            var ctx = KernelInvocationContext.Current;
            if ( ctx != null )
            {
                KernelInvocationContextExtensions.Display( ctx, $"**Search:** error: {ex.Message}", HtmlFormatter.MimeType );
            }
            return $"Error during search: {ex.Message}";
        }
    }

    /// <summary>
    /// Formats search results for inclusion in the prompt
    /// </summary>
    private string FormatSearchResults( SearchResults<SearchDocument> results, string query )
    {
        var sb = new StringBuilder();

        if ( results.TotalCount > 0 )
        {
            sb.AppendLine( $"Here is relevant information from our knowledge base for the query: '{query}'" );
            sb.AppendLine();

            int count = 1;
            foreach ( var result in results.GetResults() )
            {
                sb.AppendLine( $"Document {count}:" );
                foreach ( var kvp in result.Document )
                {
                    sb.AppendLine( $"{kvp.Key}: {kvp.Value}" );
                }
                sb.AppendLine();
                count++;
            }
        }
        else
        {
            sb.AppendLine( "No relevant documents found in the knowledge base." );
        }

        return sb.ToString();
    }
}
