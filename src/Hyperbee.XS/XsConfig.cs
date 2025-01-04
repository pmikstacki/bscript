using System.Collections.ObjectModel;
using System.Reflection;
using Hyperbee.XS.System;
using Parlot.Fluent;

namespace Hyperbee.XS;

public class XsConfig
{
    public static IReadOnlyCollection<IParseExtension> Extensions { get; set; } = ReadOnlyCollection<IParseExtension>.Empty;

    public IReadOnlyCollection<Assembly> References { get; init; } = ReadOnlyCollection<Assembly>.Empty;
}

internal static class ParserContextExtensions
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
