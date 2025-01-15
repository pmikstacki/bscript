using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class AsyncParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Complex;
    public string Key => "async";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, _, _, statement) = binder;

        return XsParsers.Bounded(
            static ctx =>
            {
                var (scope, _) = ctx;
                scope.Push( FrameType.Child );
            },
            Between(
                // This is basically a block, but we need the parts
                Terms.Char( '{' ),
                ZeroOrMany( statement ),
                Terms.Char( '}' )
            )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                return ExpressionExtensions.BlockAsync(
                    [.. scope.Variables.EnumerateValues( Collections.KeyScope.Current )],
                    [.. parts]
                );
            } ),
            static ctx =>
            {
                var (scope, _) = ctx;
                scope.Pop();
            }
        ).Named( "async" );
    }
}
