namespace Hyperbee.XS.SemanticKernel.Helpers;

/// <summary>
/// Maintains a simple chat history for the Semantic Kernel extension.
/// </summary>
public class ChatHistory
{
    private readonly List<(string role, string message)> _messages = new();

    public void AddUserMessage( string message )
    {
        _messages.Add( ("user", message) );
    }

    public void AddAssistantMessage( string message )
    {
        _messages.Add( ("assistant", message) );
    }

    public IReadOnlyList<(string role, string message)> Messages => _messages;

    public void Clear()
    {
        _messages.Clear();
    }

    public string GetUserMessages()
    {
        var userMessages = new List<string>();
        foreach ( var (role, message) in _messages )
        {
            if ( role == "user" )
                userMessages.Add( message );
        }
        return string.Join( "\n", userMessages );
    }
}

