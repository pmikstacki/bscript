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
                    if ( !method.IsDefined( typeof( ExtensionAttribute ), false ) )
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

    public MethodInfo FindMethod( Type type, string methodName, IReadOnlyList<Type> typeArgs, IReadOnlyList<Expression> args )
    {
        var candidateMethods = GetCandidateMethods( methodName, type );
        var callTypes = GetCallTypes( type, args );

        // find best match

        MethodInfo bestMatch = null;
        var bestScore = int.MaxValue;
        var ambiguousMatch = false;

        foreach ( var candidate in candidateMethods )
        {
            var extension = candidate.IsDefined( typeof(ExtensionAttribute), false );

            // Handle extension-specific slicing and type validation

            var argumentTypes = extension ? callTypes : callTypes[1..]; // extensions have an extra `this` parameter

            // Resolve open generic methods

            MethodInfo method = candidate;

            if ( candidate.IsGenericMethodDefinition )
            {
                if ( !TryResolveGenericMethod( candidate, typeArgs, argumentTypes, out method ) )
                    continue;
            }

            // Early out if the extension method 'this' is not assignable
            //
            var parameters = method.GetParameters();

            if ( extension )
            {
                var paramType = parameters[0].ParameterType;

                if ( callTypes[0] != paramType && !paramType.IsAssignableFrom( callTypes[0] ) )
                    continue;
            }

            // Compute match score

            var score = ComputeScore( argumentTypes, parameters, bestScore );

            if ( score == int.MaxValue )
                continue;

            if ( score == bestScore ) // Current best is ambiguous
                ambiguousMatch = true;

            if ( score >= bestScore )
                continue;

            bestScore = score;
            bestMatch = method;
            ambiguousMatch = false;
        }

        if ( ambiguousMatch )
            throw new AmbiguousMatchException( $"Ambiguous match for method '{methodName}'. Unable to resolve method." );

        return bestMatch;

        // helper methods
    }

    private IEnumerable<MethodInfo> GetCandidateMethods( string methodName, Type type )
    {
        var extensionMethods = _extensionMethodCache.TryGetValue( methodName, out var extensions )
            ? extensions
            : Enumerable.Empty<MethodInfo>();

        return type.GetMethods( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
            .Where( method => method.Name == methodName )
            .Concat( extensionMethods );
    }

    private static Span<Type> GetCallTypes( Type type, IReadOnlyList<Expression> args )
    {
        var span = new Type[args.Count + 1].AsSpan();

        span[0] = type; // Add `this` for extension methods
 
        for ( var i = 0; i < args.Count; i++ )
        {
            // unwrap types from expressions
            span[i+1] = args[i] is ConstantExpression constant 
                ? constant.Value?.GetType() 
                : args[i].Type;
        }

        return span;
    }

    private static bool TryResolveGenericMethod( MethodInfo method, IReadOnlyList<Type> typeArgs, ReadOnlySpan<Type> argumentTypes, out MethodInfo resolvedMethod )
    {
        // Resolve generic methods, with type inference, if needed

        resolvedMethod = method;

        var methodTypeArgs = typeArgs?.ToArray() ?? [];

        if ( methodTypeArgs.Length == 0 )
        {
            methodTypeArgs = InferGenericArguments( method, argumentTypes );

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

        return true;
    }

    private static int ComputeScore( ReadOnlySpan<Type> argumentTypes, ParameterInfo[] parameters, int bestScore )
    {
        double averagePenalty = 0.0; // Compute average penalty for all arguments using incremental average
        var paramCount = parameters.Length;

        for ( var i = 0; i < argumentTypes.Length; i++ )
        {
            if ( averagePenalty >= bestScore )
                return int.MaxValue; // Early exit if average penalty exceeds the best score

            if ( i >= paramCount )
            {
                // Handle `params` case
                if ( !parameters[^1].IsDefined( typeof(ParamArrayAttribute), false ) )
                    return int.MaxValue; // No `params` to absorb extra arguments

                var paramsElementType = parameters[^1].ParameterType.GetElementType()!;
                if ( argumentTypes[i] != null && !paramsElementType.IsAssignableFrom( argumentTypes[i] ) )
                    return int.MaxValue; // Argument not compatible with `params` array element type

                averagePenalty = ComputePenalty( averagePenalty, i, penalty: 5 ); // Penalize for using `params`
                continue;
            }

            var paramType = parameters[i].ParameterType;
            var argType = argumentTypes[i];

            if ( argType == null )
            {
                // Handle null argument
                if ( paramType.IsValueType && Nullable.GetUnderlyingType( paramType ) == null )
                    return int.MaxValue; // Null incompatible with non-nullable value types

                averagePenalty = ComputePenalty( averagePenalty, i, penalty: 2 ); // Compatible match with null
                continue;
            }

            if ( paramType == argType )
            {
                averagePenalty = ComputePenalty( averagePenalty, i, penalty: 0 ); // Exact match
            }
            else if ( paramType.IsAssignableFrom( argType ) )
            {
                averagePenalty = ComputePenalty( averagePenalty, i, penalty: 1 ); // Compatible match
            }
            else
            {
                return int.MaxValue; // No match
            }
        }

        // Handle additional parameters that are not matched by the arguments
        for ( var i = argumentTypes.Length; i < paramCount; i++ )
        {
            if ( !parameters[i].IsOptional )
                return int.MaxValue; // Missing required parameter

            averagePenalty = ((averagePenalty * i) + 2) / (i + 1); // Penalize for using a default value
        }

        return (int) Math.Round( averagePenalty );

        // Helpers

        static double ComputePenalty( double current, int count, int penalty ) => ((current * count) + penalty) / (count + 1);
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
        while ( true )
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

            if ( parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) && argumentType!.IsArray )
            {
                var elementType = argumentType.GetElementType();
                var genericArg = parameterType.GetGenericArguments()[0];

                parameterType = genericArg;
                argumentType = elementType;
                continue;
            }

            // Handle nested generic types

            if ( !parameterType.ContainsGenericParameters ) 
                return true; // Non-generic parameter, no inference needed

            if ( !parameterType.IsGenericType || !argumentType!.IsGenericType || parameterType.GetGenericTypeDefinition() != argumentType.GetGenericTypeDefinition() )
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
}
