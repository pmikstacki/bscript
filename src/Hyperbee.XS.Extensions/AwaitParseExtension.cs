using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS.System;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class AwaitParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Terminated;
    public string Key => "await";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, _, _) = binder;

        return expression
            .Then<Expression>( static parts =>
            {
                return ExpressionExtensions.Await( parts );
            } ).Named( "await" );
    }
}
