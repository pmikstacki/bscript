using System.Collections.Concurrent;
using System.Reflection;

namespace Hyperbee.XS;

internal class TypeResolver
{
    private readonly List<Assembly> _references = [typeof(string).Assembly];
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();

    public IReadOnlyCollection<Assembly> References => _references;

    public void AddReference( Assembly assembly )
    {
        _references.Add( assembly );
    }

    public void AddReferences( IReadOnlyCollection<Assembly> assemblies )
    {
        _references.AddRange( assemblies );
    }

    public Type ResolveType( string typeName )
    {
        return _typeCache.GetOrAdd( typeName, _ =>
        {
            return _references
                .SelectMany( assembly => assembly.GetTypes() )
                .FirstOrDefault( type => type.Name == typeName || type.FullName == typeName );
        } );
    }
}
