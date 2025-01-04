using System.Collections.ObjectModel;
using System.Reflection;
using Hyperbee.XS.System;

namespace Hyperbee.XS;

public class XsConfig
{
    public static IReadOnlyCollection<IParseExtension> Extensions { get; set; } = ReadOnlyCollection<IParseExtension>.Empty;

    public IReadOnlyCollection<Assembly> References { get; init; } = ReadOnlyCollection<Assembly>.Empty;
}
