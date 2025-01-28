namespace Hyperbee.XS.System.Writer;

public record ExpressionVisitorConfig(
    string Prefix = "Expression.",
    char Indentation = '\t',
    string Variable = "expression",
    params IExtensionWriter[] Writers );
