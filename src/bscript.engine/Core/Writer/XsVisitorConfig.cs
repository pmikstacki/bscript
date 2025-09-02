namespace bscript.Core.Writer;

public record XsVisitorConfig(
    string Indentation = "  ",
    params IXsWriter[] Writers );
