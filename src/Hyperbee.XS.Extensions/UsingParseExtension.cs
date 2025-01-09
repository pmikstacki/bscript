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
    public ExtensionType Type => ExtensionType.Complex;
    public string Key => "using";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, assignable, statement) = binder;

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
                var (scope, _) = ctx;
                var (variableIdentifier, disposable) = parts;

                var variableName = variableIdentifier.ToString()!;
                var variable = Variable(
                    disposable.Type,
                    variableName );

                scope.Variables.Add( variableName, variable );

                return (variable, disposable);
            } )
            .And(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( statement ),
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var ((variable, disposable), body) = parts;

                var bodyBlock = Block( body );
                return ExpressionExtensions.Using( variable, disposable, bodyBlock );
            } ).Named( "using" );
    }
}
