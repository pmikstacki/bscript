using System.Linq.Expressions;
using Hyperbee.Collections;
using System.Reflection;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.System.Parsers;

public static partial class XsParsers
{
    public static Parser<Expression> Debug( this Parser<Expression> parser ) 
    {
        return parser.Then<Expression>( ( context, statement ) =>
        {
            if ( context is not XsContext xsContext )
                throw new InvalidOperationException( $"Context must be of type {nameof( XsContext )}." );

            if ( xsContext.Debugger == null )
                return Empty();

            var span = context.Scanner.Cursor.Position;
            var captureVariables = CaptureVariables( xsContext.Scope.Variables );
            var frame = xsContext.Scope.Frame;

            var debugger = xsContext.Debugger;
            var target = debugger.Target != null
                ? Constant( debugger.Target )
                : null;

            var debugExpression = Call(
                target,
                debugger.Method,
                Constant( span.Line ),
                Constant( span.Column ),
                captureVariables,
                Constant( context.Scanner.Buffer.ShowPosition( span.Line, span.Column ) ),
                Constant( frame )
            );

            return Block( debugExpression, statement );
        } );
    }

    public static BlockExpression CaptureVariables( LinkedDictionary<string, ParameterExpression> variables )
    {
        var target = Parameter( typeof( Dictionary<string, object> ), "variables" );

        var expression = Block(
            variables: [target],
            CreateSnapshot( target, variables.EnumerateValues() )
        );

        return expression;

        static IEnumerable<Expression> CreateSnapshot( ParameterExpression target, IEnumerable<ParameterExpression> variables )
        {
            var method = typeof( Dictionary<string, object> ).GetMethod( "Add", BindingFlags.Instance | BindingFlags.Public, [typeof( string ), typeof( object )] );
            var ctor = typeof( Dictionary<string, object> ).GetConstructor( Type.EmptyTypes )!;

            yield return Assign(
                target,
                New( ctor )
            );

            foreach ( var variable in variables )
            {
                yield return Call(
                    target,
                    method!,
                    Constant( variable.Name ),
                    Convert( variable, typeof( object ) )
                );
            }

            yield return target;
        }
    }
}
