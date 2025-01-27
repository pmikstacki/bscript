namespace Hyperbee.XS.System.Writer;

public record ExpressionTreeVisitorConfig(
    string Prefix = "Expression.",
    char Indentation = '\t',
    string Variable = "expression",
    params IExtensionWriter[] Writers );
