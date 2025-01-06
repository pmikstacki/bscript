using Hyperbee.XS.System;
using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS;

public class XsContext : ParseContext
{
    public TypeResolver Resolver { get; }
    public ParseScope Scope { get; } = new();

    public XsContext( XsConfig config, Scanner scanner, bool useNewLines = false )
        : base( scanner, useNewLines )
    {
        Resolver = new TypeResolver( config?.References );
    }

    public void Deconstruct( out ParseScope scope, out TypeResolver resolver )
    {
        scope = Scope;
        resolver = Resolver;
    }
}

public static class ParseContextExtensions
{
    public static void Deconstruct( this ParseContext context, out ParseScope scope, out TypeResolver resolver )
    {
        if ( context is XsContext xsContext )
        {
            scope = xsContext.Scope;
            resolver = xsContext.Resolver;
            return;
        }

        scope = default;
        resolver = default;
    }
}

