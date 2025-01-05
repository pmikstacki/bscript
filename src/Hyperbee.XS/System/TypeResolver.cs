using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.XS.System;

public class TypeResolver
{
    private readonly List<Assembly> _references = [typeof( string ).Assembly];
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();

    public TypeResolver()
    {
    }

    public TypeResolver( IReadOnlyCollection<Assembly> references )
    {
        if ( references != null && references.Count > 0 )
            _references.AddRange( references );
    }

    public Type ResolveType( string typeName )
    {
        return _typeCache.GetOrAdd( typeName, _ =>
        {
            var type = GetTypeFromKeyword( typeName );

            if ( type != null )
                return type;

            return _references
                .SelectMany( assembly => assembly.GetTypes() )
                .FirstOrDefault( compare => compare.Name == typeName || compare.FullName == typeName );
        } );

        static Type GetTypeFromKeyword( string typeName )
        {
            // Mapping of C# keywords to their corresponding types
            return typeName switch
            {
                "int" => typeof( int ),
                "double" => typeof( double ),
                "string" => typeof( string ),
                "bool" => typeof( bool ),
                "float" => typeof( float ),
                "decimal" => typeof( decimal ),
                "object" => typeof( object ),
                "byte" => typeof( byte ),
                "char" => typeof( char ),
                "short" => typeof( short ),
                "long" => typeof( long ),
                "uint" => typeof( uint ),
                "ushort" => typeof( ushort ),
                "ulong" => typeof( ulong ),
                "sbyte" => typeof( sbyte ),
                "void" => typeof( void ),
                _ => null, // Return null for unknown types
            };
        }
    }

    public static MethodInfo FindMethod( Type type, string methodName, IReadOnlyList<Expression> args, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
    {
        return FindMethod( type, methodName, null, args, bindingAttr );
    }

    public static MethodInfo FindMethod( Type type, string methodName, IReadOnlyList<Expression> genericArgs, IReadOnlyList<Expression> args, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
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

            if ( parameters.Length != args.Count )
            {
                continue; // Skip methods with different parameter counts
            }

            var score = 0;
            var isMatch = true;

            for ( var i = 0; i < parameters.Length; i++ )
            {
                var argument = args[i];
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
