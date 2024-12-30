using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS;

public class TypeResolver
{
    private readonly List<Assembly> _references = [typeof( string ).Assembly];
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

    public static MethodInfo FindMethod( Type type, string methodName, IReadOnlyList<Expression> arguments, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
    {
        var methods = type.GetMethods( bindingAttr ).Where( m => m.Name == methodName ).ToArray();

        if ( methods.Length == 0 )
        {
            return null;
        }

        MethodInfo bestMatch = null;
        var bestScore = int.MaxValue;

        foreach ( var method in methods )
        {
            var parameters = method.GetParameters();
            if ( parameters.Length != arguments.Count )
            {
                continue; // Skip methods with different parameter counts
            }

            var score = 0;
            var isMatch = true;

            for ( var i = 0; i < parameters.Length; i++ )
            {
                var argument = arguments[i];
                var parameterType = parameters[i].ParameterType;

                // Handle null arguments
                if ( argument is ConstantExpression constant && constant.Value == null )
                {
                    // Null can match any reference type or nullable value type
                    if ( !parameterType.IsClass && Nullable.GetUnderlyingType( parameterType ) == null )
                    {
                        isMatch = false;
                        break;
                    }

                    score += 2; // Weak confidence match for null
                    continue;
                }

                // Match based on argument type
                var argumentType = argument.Type;

                if ( parameterType == argumentType )
                {
                    score += 0; // Perfect match
                }
                else if ( parameterType.IsAssignableFrom( argumentType ) )
                {
                    score += 1; // Compatible match
                }
                else
                {
                    isMatch = false;
                    break;
                }
            }

            if ( !isMatch || score >= bestScore )
            {
                continue;
            }

            bestScore = score;
            bestMatch = method;
        }

        return bestMatch;
    }

}
