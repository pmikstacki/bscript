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
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "async";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, statement) = binder;

        return XsParsers.Bounded(
            static ctx =>
            {
                ctx.EnterScope( FrameType.Block );
            },
            Between(
                // This is basically a block, but we need the parts
                Terms.Char( '{' ),
                ZeroOrMany( statement ),
                Terms.Char( '}' )
            ).Named( "async block" )
            .Then<Expression>( static ( ctx, parts ) => 
                ExpressionExtensions.BlockAsync(
                    [.. ctx.Scope().Variables.EnumerateValues( Collections.KeyScope.Current )],
                    [.. parts]
                ) 
            ),
            static ctx =>
            {
                ctx.ExitScope();
            }
        ).Named( "async" );
    }
}
