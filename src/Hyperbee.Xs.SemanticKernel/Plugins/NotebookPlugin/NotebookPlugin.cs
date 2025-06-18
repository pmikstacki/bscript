using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel;

namespace Hyperbee.XS.SemanticKernel.Plugins.NotebookPlugin;
public class NotebookPlugin
{
    [KernelFunction, Description( "Get a list of notebook chats" )]
    public static string GetNotebookHistory()
    {
        string dir = Directory.GetCurrentDirectory();
        string content = File.ReadAllText( $"{dir}/data/notebookchat.txt" );
        return content;
    }

    [KernelFunction, Description( "Add chat to list" )]
    public static string AddToNotebookList( [Description( "Chat to add" )] string chat )
    {
        try
        {
            string filePath = $"data/notebookchat.txt";
            string jsonContent = File.ReadAllText( filePath );
            var notebookHistory = JsonNode.Parse( jsonContent ) as JsonArray;

            if ( notebookHistory == null )
            {
                return string.Empty;
            }

            var newChatHistory = new JsonObject
            {
                ["chat"] = chat
            };

            notebookHistory.Insert( 0, newChatHistory );

            File.WriteAllText( filePath, JsonSerializer.Serialize( notebookHistory, new JsonSerializerOptions { WriteIndented = true } ) );
            return string.Empty;
        }
        catch ( Exception ex )
        {
            return $"Error: {ex.Message}";
        }
    }
}
