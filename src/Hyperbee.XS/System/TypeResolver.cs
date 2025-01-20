using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hyperbee.XS.System;

public class TypeResolver
{
    private readonly List<Assembly> _references = [
        typeof( string ).Assembly,
        typeof( Enumerable ).Assembly
    ];

    private readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private readonly ConcurrentDictionary<string, List<MethodInfo>> _extensionMethodCache = new();

    public TypeResolver( IReadOnlyCollection<Assembly> references )
    {
        if ( references != null && references.Count > 0 )
            _references.AddRange( references );

        CacheExtensionMethods();
    }

    private void CacheExtensionMethods()
    {
        Parallel.ForEach( _references, assembly =>
        {
            foreach ( var type in assembly.GetTypes() )
            {
                if ( !type.IsPublic || !type.IsSealed || !type.IsAbstract ) // Only static classes
                    continue;

                foreach ( var method in type.GetMethods( BindingFlags.Static | BindingFlags.Public ) )
                {
                    if ( !method.IsDefined( typeof(ExtensionAttribute), false ) )
                        continue;

                    if ( !_extensionMethodCache.TryGetValue( method.Name, out var methods ) )
                    {
                        methods = [];
                        _extensionMethodCache[method.Name] = methods;
                    }

                    methods.Add( method );
                }
            }
        } );
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

    public MethodInfo FindMethod(
        Type type,
        string methodName,
        IReadOnlyList<Type> typeArgs,
        IReadOnlyList<Expression> args )
    {
        var methods = GetCandidateMethods( methodName, type );
        var types = GetCandidateTypes( type, args );

        // find best match

        MethodInfo bestMatch = null;
        var bestScore = int.MaxValue;

        foreach ( var method in methods )
        {
            var extension = method.IsDefined( typeof(ExtensionAttribute), false );
            var argumentTypes = extension ? types : types[1..];

            if ( !TryResolveMethod( method, typeArgs, argumentTypes, out var resolvedMethod ) )
                continue;

            if ( !TryScoreMethod( resolvedMethod, argumentTypes, out var score ) )
                continue;

            if ( score == bestScore )
                throw new AmbiguousMatchException( $"Ambiguous match for method '{methodName}'. Unable to resolve method." );

            if ( score >= bestScore )
                continue;

            bestScore = score;
            bestMatch = resolvedMethod;
        }

        return bestMatch;

        // helper methods
    }

    private IEnumerable<MethodInfo> GetCandidateMethods( string methodName, Type type )
    {
        const BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        var extensionMethods = _extensionMethodCache.TryGetValue( methodName, out var extensions )
            ? extensions
            : Enumerable.Empty<MethodInfo>();

        return type.GetMethods( bindingAttr )
            .Where( method => method.Name == methodName )
            .Concat( extensionMethods );
    }

    private static Span<Type> GetCandidateTypes( Type type, IReadOnlyList<Expression> args )
    {
        var span = new Type[args.Count + 1].AsSpan();
        span[0] = type; // Add `this` for extensions
 
        for ( var i = 0; i < args.Count; i++ )
        {
            span[i+1] = args[i] is ConstantExpression constant 
                ? constant.Value?.GetType() 
                : args[i].Type;
        }

        return span;
    }

    private static bool TryResolveMethod( MethodInfo method, IReadOnlyList<Type> typeArgs, ReadOnlySpan<Type> argumentTypes, out MethodInfo resolvedMethod )
    {
        resolvedMethod = method;

        if ( !method.IsGenericMethodDefinition )
            return true;

        var methodTypeArgs = typeArgs?.ToArray() ?? [];

        if ( methodTypeArgs.Length == 0 )
        {
            methodTypeArgs = InferGenericArguments( method, argumentTypes );

            if ( methodTypeArgs == null )
                return false;
        }

        if ( method.GetGenericArguments().Length != methodTypeArgs.Length )
            return false;

        try
        {
            resolvedMethod = method.MakeGenericMethod( methodTypeArgs );
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static bool TryScoreMethod( MethodInfo method, ReadOnlySpan<Type> argumentTypes, out int score )
    {
        var parameters = method.GetParameters();

        score = 0;

        if ( parameters.Length != argumentTypes.Length )
            return false;

        int exactMatches = 0;
        int compatibleMatches = 0;
        int nullMatches = 0;

        for ( var i = 0; i < parameters.Length; i++ )
        {
            var argumentType = argumentTypes[i];

            var parameterType = parameters[i].ParameterType;

            if ( argumentType == null )
            {
                if ( !parameterType.IsClass && Nullable.GetUnderlyingType( parameterType ) == null )
                    return false;

                nullMatches++;
                continue;
            }

            if ( parameterType == argumentType )
                exactMatches++;
            
            else if ( parameterType.IsAssignableFrom( argumentType ) )
                compatibleMatches++; // Compatible match

            else
                return false;
        }

        score = (nullMatches * 10) + (compatibleMatches * 5) + exactMatches;
        return true;
    }

    private static Type[] InferGenericArguments( MethodInfo method, ReadOnlySpan<Type> argumentTypes )
    {
        var genericParameters = method.GetGenericArguments();
        var inferredTypes = new Type[genericParameters.Length];

        var parameters = method.GetParameters();
        var argumentCount = argumentTypes.Length;

        for ( var i = 0; i < parameters.Length; i++ )
        {
            var parameter = parameters[i];

            if ( i >= argumentCount )
            {
                if ( !parameter.HasDefaultValue )
                    return null; // Missing argument for non-default parameter

                continue; // Skip inference for default parameters
            }

            var argumentType = argumentTypes[i];

            if ( TryInferTypes( parameter.ParameterType, argumentType, genericParameters, inferredTypes ) )
                continue;

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
                inferredTypes[index] = argumentType; // Infer the type
 
            else if ( inferredTypes[index] != argumentType )
                return false; // Ambiguous inference

            return true;
        }

        // Handle array types explicitly for IEnumerable<T>

        if ( parameterType.IsGenericType &&
             parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
             argumentType.IsArray )
        {
            var elementType = argumentType.GetElementType();
            var genericArg = parameterType.GetGenericArguments()[0];

            return TryInferTypes( genericArg, elementType, genericParameters, inferredTypes );
        }

        // Handle nested generic types

        if ( !parameterType.ContainsGenericParameters )
            return true; // Non-generic parameter, no inference needed

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
