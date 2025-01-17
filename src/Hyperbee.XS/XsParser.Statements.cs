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
    private static Parser<IReadOnlyList<Expression>> BlockStatementParser( Deferred<Expression> statement )
    {
        return OneOf(
            statement.Then<IReadOnlyList<Expression>>( x => new List<Expression> { x } ), // Single statement as a sequence
            Between(
                Terms.Char( '{' ),
                ZeroOrMany( statement ),
                Terms.Char( '}' )
            )
        );
    }

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
            .And(
                BlockStatementParser( statement )
            )
            .And(
                ZeroOrOne(
                    Terms.Text( "else" )
                    .SkipAnd(
                        BlockStatementParser( statement )
                    )
                )
            )
            .Then<Expression>( static parts =>
            {
                var (test, trueExprs, falseExprs) = parts;

                var ifTrue = ConvertToSingleExpression( trueExprs );
                var ifFalse = ConvertToSingleExpression( ifTrue?.Type, falseExprs );

                var type = ifTrue!.Type;

                return Condition( test, ifTrue, ifFalse, type );
            } )
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
            .And(
                BlockStatementParser( statement )
            )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                var ((breakLabel, continueLabel), exprs) = parts;

                try
                {
                    var body = Block( exprs );
                    return Loop( body, breakLabel, continueLabel );
                }
                finally
                {
                    scope.Pop();
                }
            } )
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
                } );
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
                } );
        }

        static Parser<string> EndCase()
        {
            return Terms.Text( "case" ).Or( Terms.Text( "default" ) ).Or( Terms.Text( "}" ) );
        }
    }

    private static KeyParserPair<Expression> TryCatchParser( Deferred<Expression> statement )
    {
        return new( "try",
            BlockStatementParser( statement )
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
                        .And(
                            BlockStatementParser( statement ) 
                        )
                    )
                )
            )
            .And(
                ZeroOrOne(
                    Terms.Text( "finally" )
                    .SkipAnd(
                        BlockStatementParser( statement )
                    )
                )
            )
            .Then<Expression>( static parts =>
            {
                var (tryParts, catchParts, finallyParts) = parts;

                var tryType = tryParts?[^1].Type ?? typeof( void );

                var catchBlocks = catchParts.Select( part =>
                {
                    var (exceptionVariable, catchBody) = part;

                    return Catch(
                        exceptionVariable,
                        Block( tryType, catchBody )
                    );
                } ).ToArray();

                var tryBlock = ConvertToSingleExpression( tryType, tryParts );
                var finallyBlock = ConvertToSingleExpression( finallyParts );

                return TryCatchFinally(
                    tryBlock,
                    finallyBlock,
                    catchBlocks
                );
            } )
        );
    }

    private static KeyParserPair<Expression> NewParser( Parser<Expression> expression )
    {
        var objectConstructor =
            Between(
                Terms.Char( '(' ),
                ArgsParser( expression ),
                Terms.Char( ')' )
            ).Then( static parts =>
                (ConstructorType.Object, parts, (IReadOnlyList<Expression>) null)
            );

        var arrayConstructor =
            Between(
                Terms.Char( '[' ),
                ZeroOrOne( Separated(
                    Terms.Char( ',' ),
                    expression
                ) ),
                Terms.Char( ']' )
            )
            .And(
                ZeroOrOne(
                    Between(
                        Terms.Char( '{' ),
                        Separated(
                            Terms.Char( ',' ),
                            expression
                        ),
                        Terms.Char( '}' )
                    )
                )
            )
            .Then( static parts =>
            {
                var (bounds, initial) = parts;

                return initial == null
                    ? (ConstructorType.ArrayBounds, bounds, null)
                    : (ConstructorType.ArrayInit, bounds, initial);
            } );


        return new ( "new",
            //.SkipAnd( TypeRuntime() )
            TypeRuntime()
            .And( OneOf( objectConstructor, arrayConstructor ) )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (type, (constructorType, arguments, initial)) = parts;

                switch ( constructorType )
                {
                    case ConstructorType.ArrayBounds:
                        if ( arguments.Count == 0 )
                            throw new InvalidOperationException( "Array bounds initializer requires at least one argument." );

                        return NewArrayBounds( type, arguments );

                    case ConstructorType.ArrayInit:
                        var arrayType = initial[^1].Type;

                        if ( type != arrayType && arrayType.IsArray && type != arrayType.GetElementType() )
                            throw new InvalidOperationException( $"Array of type {type.Name} does not match type {arrayType.Name}." );

                        return NewArrayInit( arrayType, initial );

                    case ConstructorType.Object:
                        var constructor = type.GetConstructor( arguments.Select( arg => arg.Type ).ToArray() );

                        if ( constructor == null )
                            throw new InvalidOperationException( $"No matching constructor found for type {type.Name}." );

                        return New( constructor, arguments );

                    default:
                        throw new InvalidOperationException( $"Unsupported constructor type: {constructorType}." );
                }
            }
        ) );
    }

    private enum ConstructorType
    {
        Object,
        ArrayBounds,
        ArrayInit,
    }
}
