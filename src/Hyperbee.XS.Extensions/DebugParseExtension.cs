using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Collections;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Parlot.Fluent;

namespace Hyperbee.Xs.Extensions;

public class DebugParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Terminated;
    public string Key => "debug";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, _) = binder;

        return Parsers.Between(
                Parsers.Terms.Char( '(' ),
                Parsers.ZeroOrOne( expression ),
                Parsers.Terms.Char( ')' )
            )
            .AndSkip( Parsers.Terms.Char( ';' ) )
            .Then<Expression>( ( context, condition ) =>
            {
                if ( context is not XsContext xsContext )
                    throw new InvalidOperationException( $"Context must be of type {nameof(XsContext)}." );

                if ( xsContext.Debugger == null )
                    return Expression.Empty();

                var span = context.Scanner.Cursor.Position;
                var captureVariables = CaptureVariables( xsContext.Scope.Variables );
                var frame = xsContext.Scope.Frame;

                var debugger = xsContext.Debugger;
                var target = debugger.Target != null
                    ? Expression.Constant( debugger.Target )
                    : null;

                var debugExpression = Expression.Call(
                    target, 
                    debugger.Method,
                    Expression.Constant( span.Line ),
                    Expression.Constant( span.Column ),
                    captureVariables,
                    Expression.Constant( frame )
                );

                return (condition != null)
                    ? Expression.IfThen( condition, debugExpression )
                    : debugExpression;
            } );
    }

    private static BlockExpression CaptureVariables( LinkedDictionary<string, ParameterExpression> variables )
    {
        var target = Expression.Parameter( typeof(Dictionary<string, object>), "variables" );

        var expression = Expression.Block(
            variables: [target],
            CreateSnapshot( target, variables.EnumerateValues() )
        );

        return expression;

        static IEnumerable<Expression> CreateSnapshot( ParameterExpression target, IEnumerable<ParameterExpression> variables )
        {
            var method = typeof( Dictionary<string, object> ).GetMethod( "Add", BindingFlags.Instance | BindingFlags.Public, [typeof(string), typeof(object)] );
            var ctor = typeof(Dictionary<string, object>).GetConstructor( System.Type.EmptyTypes )!;

            yield return Expression.Assign(
                target,
                Expression.New( ctor )
            );

            foreach ( var variable in variables )
            {
                yield return Expression.Call(
                    target,
                    method!,
                    Expression.Constant( variable.Name ),
                    Expression.Convert( variable, typeof(object) )
                );
            }

            yield return target;
        }
    }
}
