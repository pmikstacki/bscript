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

    public static MethodInfo FindMethod( 
        Type type, 
        string methodName, 
        IReadOnlyList<Type> typeArgs, 
        IReadOnlyList<Expression> args, 
        BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
    {
        var methods = type.GetMethods( bindingAttr ).Where( method => method.Name == methodName ).ToArray();

        if ( methods.Length == 0 )
        {
            return null;
        }

        MethodInfo bestMatch = null;
        var bestScore = int.MaxValue;

        foreach ( var method in methods )
        {
            if ( !TryResolveMethod( method, typeArgs, args, out var resolvedMethod ) )
            {
                continue;
            }

            if ( !TryScoreMethod( resolvedMethod, args, out var score ) )
            {
                continue;
            }

            if ( score == bestScore )
            {
                throw new AmbiguousMatchException(
                    $"Ambiguous match for method '{methodName}'. Unable to resolve method." );
            }

            if ( score < bestScore )
            {
                bestScore = score;
                bestMatch = resolvedMethod;
            }
        }

        return bestMatch;
    }

    private static bool TryResolveMethod( MethodInfo method, IReadOnlyList<Type> typeArgs, IReadOnlyList<Expression> args, out MethodInfo resolvedMethod )
    {
        resolvedMethod = method;

        var methodTypeArgs = typeArgs?.ToArray() ?? [];

        if ( method.IsGenericMethodDefinition )
        {
            if ( methodTypeArgs.Length == 0 )
            {
                methodTypeArgs = InferGenericArguments( method, args );

                if ( methodTypeArgs == null )
                {
                    return false;
                }
            }

            if ( method.GetGenericArguments().Length != methodTypeArgs.Length )
            {
                return false;
            }

            try
            {
                resolvedMethod = method.MakeGenericMethod( methodTypeArgs );
            }
            catch
            {
                return false;
            }
        }
        else if ( methodTypeArgs.Length > 0 )
        {
            return false;
        }

        return true;
    }

    private static bool TryScoreMethod( MethodInfo method, IReadOnlyList<Expression> args, out int score )
    {
        var parameters = method.GetParameters();
        score = 0;

        if ( parameters.Length != args.Count )
        {
            return false;
        }

        for ( var i = 0; i < parameters.Length; i++ )
        {
            var argument = args[i];
            var parameterType = parameters[i].ParameterType;

            if ( argument is ConstantExpression constant && constant.Value == null )
            {
                if ( !parameterType.IsClass && Nullable.GetUnderlyingType( parameterType ) == null )
                {
                    return false;
                }

                score += 2; // Weak confidence match for null
                continue;
            }

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
                return false;
            }
        }

        return true;
    }

    private static Type[] InferGenericArguments( MethodInfo method, IReadOnlyList<Expression> args )
    {
        var genericParameters = method.GetGenericArguments();
        var inferredTypes = new Type[genericParameters.Length];

        foreach ( var (parameterType, argumentType) in method.GetParameters().Select( ( p, i ) => (p.ParameterType, args[i].Type) ) )
        {
            if ( TryInferTypes( parameterType, argumentType, genericParameters, inferredTypes ) )
            {
                continue;
            }

            return null;
        }

        return inferredTypes;
    }

    private static bool TryInferTypes( Type parameterType, Type argumentType, Type[] genericParameters, Type[] inferredTypes )
    {
        // Handle direct generic parameters

        if ( parameterType.IsGenericParameter )
        {
            var index = Array.IndexOf( genericParameters, parameterType );

            if ( index < 0 ) 
                return true; // Not relevant

            if ( inferredTypes[index] == null )
            {
                inferredTypes[index] = argumentType; // Infer the type
            }
            else if ( inferredTypes[index] != argumentType )
            {
                return false; // Ambiguous inference
            }

            return true;
        }

        // Handle nested generic types

        if ( !parameterType.ContainsGenericParameters )
        {
            return true; // Non-generic parameter, no inference needed
        }

        if ( !parameterType.IsGenericType || !argumentType.IsGenericType ||
             parameterType.GetGenericTypeDefinition() != argumentType.GetGenericTypeDefinition() )
        {
            return false;
        }

        var parameterArgs = parameterType.GetGenericArguments();
        var argumentArgs = argumentType.GetGenericArguments();

        for ( var i = 0; i < parameterArgs.Length; i++ )
        {
            if ( TryInferTypes( parameterArgs[i], argumentArgs[i], genericParameters, inferredTypes ) )
                continue;

            return false;
        }

        return true;
    }
}
