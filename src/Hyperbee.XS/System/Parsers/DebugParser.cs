using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Collections;
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
                XsParsersHelper.CaptureVariables( xsContext.Scope.Variables ),
                Constant( context.Scanner.Buffer.ShowPosition( span.Line, span.Column ) ),
                Constant( frame )
            );

            return Block( debugExpression, statement );
        } );
    }
}

public static class XsParsersHelper
{
    private static readonly MethodInfo AddMethod;
    private static readonly ConstructorInfo Constructor;

    static XsParsersHelper()
    {
        AddMethod = typeof( Dictionary<string, object> ).GetMethod( "Add", BindingFlags.Instance | BindingFlags.Public, [typeof( string ), typeof( object )] );
        Constructor = typeof( Dictionary<string, object> ).GetConstructor( Type.EmptyTypes )!;
    }

    public static Expression CaptureVariables( LinkedDictionary<string, ParameterExpression> variables )
    {
        return ListInit(
            New( Constructor ),
            variables.Select(
                kvp => ElementInit(
                    AddMethod,
                    Constant( kvp.Key ),
                    Convert( kvp.Value, typeof( object ) )
                )
            ) );
    }
}
