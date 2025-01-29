using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS;
using Hyperbee.XS.System;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class UsingParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "using";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return
            Between(
                Terms.Char( '(' ),
                Terms.Text( "var" )
                    .SkipAnd( Terms.Identifier() )
                    .AndSkip( Terms.Text( "=" ) )
                    .And( expression ),
                Terms.Char( ')' )
            ).Then( static ( ctx, parts ) =>
            {
                var (variableIdentifier, disposable) = parts;

                var variableName = variableIdentifier.ToString()!;
                var variable = Variable(
                    disposable.Type,
                    variableName );

                ctx.Scope().Variables.Add( variableName, variable );

                return (variable, disposable);
            } )
            .And( statement )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var ((variable, disposable), body) = parts;

                return ExpressionExtensions.Using( variable, disposable, body );
            } ).Named( "using" );
    }
}
