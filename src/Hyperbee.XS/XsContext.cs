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
