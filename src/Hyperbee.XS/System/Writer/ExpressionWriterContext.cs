using System.Linq.Expressions;

namespace Hyperbee.XS.System.Writer;

public class ExpressionWriterContext
{
    internal readonly HashSet<string> Usings = [
        "System",
        "System.Linq.Expressions",
    ];

    internal readonly Dictionary<ParameterExpression, string> Parameters = [];
    internal readonly Dictionary<LabelTarget, string> Labels = [];

    internal readonly StringWriter ParameterOutput = new();
    internal readonly StringWriter LabelOutput = new();
    internal readonly StringWriter ExpressionOutput = new();

    internal int IndentDepth = 0;

    internal char Indention => Config.Indentation;
    internal string Prefix => Config.Prefix;

    internal IExtensionWriter[] ExtensionWriters => Config.Writers;

    internal ExpressionTreeVisitor Visitor { get; init; }
    internal ExpressionTreeVisitorConfig Config { get; init; }

    internal ExpressionWriterContext(
        ExpressionTreeVisitor visitor,
        StringWriter parameterOutput = null,
        StringWriter labelOutput = null,
        StringWriter expressionOutput = null,
        ExpressionTreeVisitorConfig config = null )
    {
        Visitor = visitor;
        ParameterOutput = parameterOutput ?? new();
        LabelOutput = labelOutput ?? new();
        ExpressionOutput = expressionOutput ?? new();
        Config = config ?? new();
    }

    public ExpressionWriter EnterExpression( string name, bool newLine = true, bool prefix = true )
    {
        var writer = new ExpressionWriter( this, ( w ) => ExitExpression( w, newLine ) );

        writer.Write( $"{(prefix ? Prefix : string.Empty)}{name}(", indent: true );

        if ( newLine )
            writer.Write( "\n" );

        writer.Indent();

        return writer;
    }

    public ExpressionWriter GetWriter()
    {
        return new ExpressionWriter( this, null );
    }

    private void ExitExpression( ExpressionWriter writer, bool newLine = true )
    {
        writer.Outdent();

        if ( newLine )
        {
            writer.Write( "\n" );
            writer.Write( ")", indent: true );
        }
        else
        {
            writer.Write( ")" );
        }
    }
}
