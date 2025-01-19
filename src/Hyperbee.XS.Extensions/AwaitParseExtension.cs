using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS.System;
using Parlot.Fluent;

namespace Hyperbee.Xs.Extensions;

public class AwaitParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "await";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, _) = binder;

        return expression
            .Then<Expression>( static parts => ExpressionExtensions.Await( parts ) )
            .Named( "await" );
    }
}
