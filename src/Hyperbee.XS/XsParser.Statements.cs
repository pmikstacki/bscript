using System.Linq.Expressions;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Hyperbee.XS.System.Parsers.XsParsers;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

public partial class XsParser
{
    // Terminated Statement Parsers

    private static KeyParserPair<Expression> BreakParser()
    {
        return new( "break",
            Always()
            .Then<Expression>( static ( ctx, _ ) =>
            {
                var (scope, _) = ctx;
                var breakLabel = scope.Frame.BreakLabel;

                if ( breakLabel == null )
                    throw new Exception( "Invalid use of 'break' outside of a loop or switch." );

                return Break( breakLabel );
            } )
            .AndSkip( Terms.Char( ';' ) )
        );
    }

    private static KeyParserPair<Expression> ContinueParser()
    {
        return new( "continue",
            Always().Then<Expression>( static ( ctx, _ ) =>
            {
                var (scope, _) = ctx;
                var continueLabel = scope.Frame.ContinueLabel;

                if ( continueLabel == null )
                    throw new Exception( "Invalid use of 'continue' outside of a loop." );

                return Continue( continueLabel );
            } )
            .AndSkip( Terms.Char( ';' ) )
        );
    }

    private static KeyParserPair<Expression> GotoParser()
    {
        return new( "goto",
            Terms.Identifier()
            .Then<Expression>( static ( ctx, labelName ) =>
            {
                var (scope, _) = ctx;
                var label = scope.Frame.GetOrCreateLabel( labelName.ToString() );
                return Goto( label );
            } )
            .AndSkip( Terms.Char( ';' ) )
        );
    }

    private static Parser<Expression> LabelParser()
    {
        return Terms.Identifier()
            .AndSkip( Terms.Char( ':' ) )
            .AndSkip( Literals.WhiteSpace( includeNewLines: true ) )
            .Then<Expression>( static ( ctx, labelName ) =>
            {
                var (scope, _) = ctx;

                var label = scope.Frame.GetOrCreateLabel( labelName.ToString() );
                return Label( label );
            }
        );
    }

    private static KeyParserPair<Expression> ReturnParser( Parser<Expression> expression )
    {
        return new( "return",
            ZeroOrOne( expression )
            .Then<Expression>( static ( ctx, returnValue ) =>
            {
                var (scope, _) = ctx;

                var returnType = returnValue?.Type ?? typeof( void );
                var returnLabel = scope.Frame.GetOrCreateReturnLabel( returnType );

                return returnType == typeof( void )
                    ? Return( returnLabel )
                    : Return( returnLabel, returnValue, returnType );
            } )
            .AndSkip( Terms.Char( ';' ) )
        );
    }

    private static KeyParserPair<Expression> ThrowParser( Parser<Expression> expression )
    {
        return new( "throw",
            ZeroOrOne( expression )
            .Then<Expression>( static exceptionExpression =>
            {
                if ( exceptionExpression != null && !typeof( Exception ).IsAssignableFrom( exceptionExpression.Type ) )
                {
                    throw new InvalidOperationException(
                        $"Invalid throw argument: Expected an exception type, but found {exceptionExpression.Type}." );
                }

                return Throw( exceptionExpression );
            } )
            .AndSkip( Terms.Char( ';' ) )
        );
    }

    // Compound Statement Parsers

    private static KeyParserPair<Expression> ConditionalParser( Parser<Expression> expression, Deferred<Expression> statement )
    {
        return new( "if",
            Between(
                Terms.Char( '(' ),
                expression,
                Terms.Char( ')' )
            )
            .And( statement )
            .And(
                ZeroOrOne(
                    Terms.Text( "else" )
                    .SkipAnd( statement )
                )
            )
            .Then<Expression>( static parts =>
            {
                var (test, trueExprs, falseExprs) = parts;

                var type = trueExprs.Type;
                return Condition( test, trueExprs, falseExprs ?? Default( type ), type );
            } )
            .Named( "if" )
        );
    }
    private static KeyParserPair<Expression> LoopParser( Deferred<Expression> statement )
    {
        return new( "loop",
            Always().Then( ( ctx, _ ) =>
            {
                var (scope, _) = ctx;

                var breakLabel = Label( typeof( void ), "Break" );
                var continueLabel = Label( typeof( void ), "Continue" );

                scope.Push( FrameType.Child, breakLabel, continueLabel );

                return (breakLabel, continueLabel);
            } )
            .And( statement )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                var ((breakLabel, continueLabel), body) = parts;

                try
                {
                    return Loop( body, breakLabel, continueLabel );
                }
                finally
                {
                    scope.Pop();
                }
            } )
            .Named( "loop" )
        );
    }

    private static KeyParserPair<Expression> SwitchParser( Parser<Expression> expression, Deferred<Expression> statement )
    {
        return new( "switch",
            Always().Then( static ( ctx, _ ) =>
            {
                var (scope, _) = ctx;

                var breakLabel = Label( typeof( void ), "Break" );
                scope.Push( FrameType.Child, breakLabel );

                return breakLabel;
            } )
            .And(
                Between(
                    Terms.Char( '(' ),
                    expression,
                    Terms.Char( ')' )
                )
            )
            .And(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( Case( expression, statement ) )
                        .And( ZeroOrOne( Default( statement ) ) ),
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                var (breakLabel, switchValue, bodyParts) = parts;

                try
                {
                    var (cases, defaultBody) = bodyParts;

                    return Block(
                        Switch( switchValue, defaultBody, cases.ToArray() ),
                        Label( breakLabel )
                    );
                }
                finally
                {
                    scope.Pop();
                }
            } )
            .Named( "switch" )
        );

        static Parser<SwitchCase> Case( Parser<Expression> expression, Deferred<Expression> statement )
        {
            return Terms.Text( "case" )
                .SkipAnd( expression )
                .AndSkip( Terms.Char( ':' ) )
                .And(
                    ZeroOrMany( BreakOn( EndCase(), statement ) )
                )
                .Then( static parts =>
                {
                    var (testExpression, statements) = parts;
                    var body = ConvertToSingleExpression( statements );

                    return SwitchCase( body, testExpression );
                } )
                .Named( "case" );
        }

        static Parser<Expression> Default( Deferred<Expression> statement )
        {
            return Terms.Text( "default" )
                .SkipAnd( Terms.Char( ':' ) )
                .SkipAnd( ZeroOrMany( statement ) )
                .Then( static statements =>
                {
                    var body = ConvertToSingleExpression( statements );
                    return body;
                } )
                .Named( "default case" );
        }

        static Parser<string> EndCase()
        {
            return Terms.Text( "case" ).Or( Terms.Text( "default" ) ).Or( Terms.Text( "}" ) );
        }
    }

    private static KeyParserPair<Expression> TryCatchParser( Deferred<Expression> statement )
    {
        return new( "try",
            statement
            .And(
                ZeroOrMany(
                    Terms.Text( "catch" )
                    .SkipAnd(
                        Between(
                            Terms.Char( '(' ),
                            Terms.Identifier().And( ZeroOrOne( Terms.Identifier() ) ),
                            Terms.Char( ')' )
                        )
                        .Then( static ( ctx, parts ) =>
                        {
                            var (_, resolver) = ctx;
                            var (typeName, variableName) = parts;

                            var type = resolver.ResolveType( typeName.ToString()! );

                            if ( type == null )
                                throw new InvalidOperationException( $"Unknown type: {typeName}." );

                            var name = variableName.Length == 0 ? null : variableName.ToString();

                            return Parameter( type, name );
                        } )
                        .And( statement )
                    )
                )
            )
            .And(
                ZeroOrOne(
                    Terms.Text( "finally" )
                    .SkipAnd( statement )
                )
            )
            .Then<Expression>( static parts =>
            {
                var (tryParts, catchParts, finallyParts) = parts;

                var tryType = tryParts?.Type ?? typeof( void );

                var catchBlocks = catchParts.Select( part =>
                {
                    var (exceptionVariable, catchBody) = part;

                    return Catch(
                        exceptionVariable,
                        Block( tryType, catchBody )
                    );
                } ).ToArray();

                return TryCatchFinally(
                    tryParts,
                    finallyParts,
                    catchBlocks
                );

            } )
            .Named( "try" )
        );
    }
}
