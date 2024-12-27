using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hyperbee.Collections;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.Parsers;

public interface IParserExtension
{
    void Extend( Parser<Expression> parser );
}

public class XsParser
{
    private Parser<Expression> _xs;
    private readonly List<IParserExtension> _extensions = [];
    private readonly Dictionary<string, MethodInfo> _methodTable;

    private Scope Scope { get; } = new();

    public XsParser( Dictionary<string, MethodInfo> methodTable = null )
    {
        _methodTable = methodTable ?? new Dictionary<string, MethodInfo>();
        InitializeParser();
    }

    public void AddExtension( IParserExtension extension )
    {
        _extensions.Add( extension );
    }

    public Expression Parse( string script )
    {
        var scanner = new Parlot.Scanner( script );
        var context = new ParseContext( scanner ) { WhiteSpaceParser = XsParsers.WhitespaceOrNewLineOrComment() };

        return _xs.Parse( context );
    }

    // Parser TODO
    //
    // Add Import statements //BF ME discuss - how should we include and resolve types
    // Add Extensions
    // Compile //BF ME discuss
    //
    // Add New
    // Add Method calls
    // Add Lambda expressions //BF ME discuss
    // Add Member access
    // Add Return //BF ME discuss - synthesize method and visitor requirement
    // Add Throw
    // Add Indexer access

    private void InitializeParser()
    {
        // Deferred parser for recursive expressions
        var expression = Deferred<Expression>();
        var statement = Deferred<Expression>();

        // Literals

        var integerLiteral = Terms.Number<int>( NumberOptions.AllowLeadingSign ).Then<Expression>( value => Constant( value ) );
        var longLiteral = Terms.Number<long>( NumberOptions.AllowLeadingSign ).Then<Expression>( value => Constant( value ) );
        var floatLiteral = Terms.Number<float>( NumberOptions.Float ).Then<Expression>( value => Constant( value ) );
        var doubleLiteral = Terms.Number<double>( NumberOptions.Float ).Then<Expression>( value => Constant( value ) );

        var stringLiteral = Terms.String().Then<Expression>( value => Constant( value.ToString() ) );
        var booleanLiteral = Terms.Text( "true" ).Or( Terms.Text( "false" ) ).Then<Expression>( value => Constant( bool.Parse( value ) ) );
        var nullLiteral = Terms.Text( "null" ).Then<Expression>( _ => Constant( null ) );

        var literal = OneOf(
            integerLiteral,
            longLiteral,
            floatLiteral,
            doubleLiteral,
            stringLiteral,
            booleanLiteral,
            nullLiteral
        ).Named( "literal" );

        // Identifiers

        var primaryIdentifier = Terms.Identifier().Then<Expression>( Scope.LookupVariable );

        var prefixIdentifier = OneOf( Terms.Text( "++" ), Terms.Text( "--" ) )
            .And( Terms.Identifier() )
            .Then<Expression>( parts =>
            {
                var (op, ident) = parts;
                var variable = Scope.LookupVariable( ident );

                return op switch
                {
                    "++" => PreIncrementAssign( variable ),
                    "--" => PreDecrementAssign( variable ),
                    _ => throw new InvalidOperationException( $"Unsupported prefix operator: {op}." )
                };
            } );

        var postfixIdentifier = Terms.Identifier()
            .And( OneOf( Terms.Text( "++" ), Terms.Text( "--" ) ) )
            .Then<Expression>( parts =>
            {
                var (ident, op) = parts;
                var variable = Scope.LookupVariable( ident );

                return op switch
                {
                    "++" => PostIncrementAssign( variable ),
                    "--" => PostDecrementAssign( variable ),
                    _ => throw new InvalidOperationException( $"Unsupported postfix operator: {op}." )
                };
            } );

        var identifier = OneOf(
            prefixIdentifier,
            postfixIdentifier,
            primaryIdentifier
        ).Named( "identifier" );

        // Grouped Expressions

        var groupedExpression = Between(
            Terms.Char( '(' ),
            expression,
            Terms.Char( ')' )
        ).Named( "group" );

        // Primary Expressions

        var primaryExpression = OneOf(
            literal,
            identifier,
            groupedExpression
        ).Named( "primary" );

        // Unary Expressions

        var unaryExpression = primaryExpression.Unary(
            (Terms.Char( '!' ), Not),
            (Terms.Char( '-' ), Negate)
        ).Named( "unary" );

        // Binary Expressions

        var binaryExpression = unaryExpression.LeftAssociative(
            (Terms.Text( "*" ), Multiply),
            (Terms.Text( "/" ), Divide),
            (Terms.Text( "+" ), Add),
            (Terms.Text( "-" ), Subtract),
            (Terms.Text( "==" ), Equal),
            (Terms.Text( "!=" ), NotEqual),
            (Terms.Text( "<" ), LessThan),
            (Terms.Text( ">" ), GreaterThan),
            (Terms.Text( "<=" ), LessThanOrEqual),
            (Terms.Text( ">=" ), GreaterThanOrEqual),
            (Terms.Text( "&&" ), AndAlso),
            (Terms.Text( "||" ), OrElse),
            (Terms.Text( "??" ), Coalesce)
        ).Named( "binary" );

        expression.Parser = OneOf(
            binaryExpression
        );

        // Variable Declarations

        var declaration = Terms.Text( "var" )
            .SkipAnd( Terms.Identifier() )
            .AndSkip( Terms.Char( '=' ) )
            .And( expression )
            .Then<Expression>( parts =>
            {
                var (ident, right) = parts;
                var left = ident.ToString()!;

                var variable = Variable( right.Type, left );
                Scope.Variables.Add( left, variable );

                return Assign( variable, right );
            }
        ).Named( "declaration" );

        // Assignments

        var assignment =
            Terms.Identifier()
            .And(
                SkipWhiteSpace(
                    Terms.Text( "=" )
                    .Or( Terms.Text( "+=" ) )
                    .Or( Terms.Text( "-=" ) )
                    .Or( Terms.Text( "*=" ) )
                    .Or( Terms.Text( "/=" ) )
                    .Or( Terms.Text( "??=" ) )
                )
            )
            .And( expression )
            .Then<Expression>( parts =>
                {
                    var (ident, op, right) = parts;
                    var left = Scope.LookupVariable( ident );

                    return op switch
                    {
                        "=" => Assign( left, right ),
                        "+=" => AddAssign( left, right ),
                        "-=" => SubtractAssign( left, right ),
                        "*=" => MultiplyAssign( left, right ),
                        "/=" => DivideAssign( left, right ),
                        "??=" => Assign( left, Coalesce( left, right ) ),
                        _ => throw new InvalidOperationException( $"Unsupported operator: {op}." )
                    };
                }
            ).Named( "assignment" );

        // Statements

        var conditionalStatement = ConditionalParser( expression, statement );
        var loopStatement = LoopParser( statement );
        var tryCatchStatement = TryCatchParser( statement );
        var switchStatement = SwitchParser( expression, statement );

        var breakStatement = BreakParser();
        var continueStatement = ContinueParser();
        var gotoStatement = GotoParser();
        var labelStatement = LabelParser();

        //var methodCall = MethodCallParser( expression, identifier );
        //var lambdaInvocation = LambdaInvokeParser( expression, identifier );

        var complexStatement = OneOf( // Complex statements are statements that control scope or flow
            conditionalStatement,
            loopStatement,
            tryCatchStatement,
            switchStatement
        );

        var expressionStatement = OneOf( // Expression statements are single-line statements that are semicolon terminated
            breakStatement,
            continueStatement,
            gotoStatement,
            //methodCall
            //lambdaInvocation
            declaration,
            assignment,
            expression
        ).AndSkip( Terms.Char( ';' ) );

        statement.Parser = OneOf(
            complexStatement,
            labelStatement,
            expressionStatement
        );

        // Finalize

        _xs = Between(
                Always().Then<Expression>( _ =>
                {
                    Scope.Push( new Frame() );
                    return null;
                } ),
                ZeroOrMany( statement ).Then<Expression>( statements =>
                    Block(
                        Scope.Variables.EnumerateValues(),
                        statements
                    )
                ),
                Always<Expression>().Then<Expression>( _ =>
                {
                    Scope.Pop();
                    return null;
                } )
            );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Expression ConvertToSingleExpression( IReadOnlyCollection<Expression> expressions )
    {
        return ConvertToSingleExpression( expressions, typeof( void ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Expression ConvertToSingleExpression( IReadOnlyCollection<Expression> expressions, Type defaultType )
    {
        return expressions?.Count switch
        {
            null or 0 => defaultType == null ? null : Default( defaultType ),
            1 => expressions.First(),
            _ => Block( expressions )
        };
    }

    private Parser<Expression> BreakParser()
    {
        return Terms.Text( "break" )
            .Then<Expression>( _ =>
            {
                var breakLabel = Scope.Frame.BreakLabel;

                if ( breakLabel == null )
                    throw new Exception( "Invalid use of 'break' outside of a loop or switch." );

                return Break( breakLabel );
            } );
    }

    private Parser<Expression> ContinueParser()
    {
        return Terms.Text( "continue" )
            .Then<Expression>( _ =>
            {
                var continueLabel = Scope.Frame.ContinueLabel;

                if ( continueLabel == null )
                    throw new Exception( "Invalid use of 'continue' outside of a loop." );

                return Continue( continueLabel );
            } );
    }

    private Parser<Expression> GotoParser()
    {
        return Terms.Text( "goto" )
            .SkipAnd( Terms.Identifier() )
            .Then<Expression>( labelName =>
            {
                var label = Scope.Frame.GetOrCreateLabel( labelName.ToString() );
                return Goto( label );
            } );
    }

    private Parser<Expression> LabelParser()
    {
        return Terms.Identifier()
            .AndSkip( Terms.Char( ':' ) )
            .AndSkip( Literals.WhiteSpace( includeNewLines: true ) )
            .Then<Expression>( labelName =>
            {
                var label = Scope.Frame.GetOrCreateLabel( labelName.ToString() );
                return Label( label );
            } );
    }

    private Parser<Expression> ConditionalParser( Deferred<Expression> expression, Deferred<Expression> statement )
    {
        var parser = Terms.Text( "if" )
            .SkipAnd(
                Between(
                    Terms.Char( '(' ),
                    expression,
                    Terms.Char( ')' )
                )
            )
            .And(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( statement ),
                    Terms.Char( '}' )
                )
            )
            .And( ZeroOrOne(
                Terms.Text( "else" )
                    .SkipAnd(
                        Between(
                            Terms.Char( '{' ),
                            ZeroOrMany( statement ),
                            Terms.Char( '}' )
                        )
                    )
                )
            )
            .Then<Expression>( parts =>
            {
                var (test, trueExprs, falseExprs) = parts;

                var ifTrue = ConvertToSingleExpression( trueExprs );
                var ifFalse = ConvertToSingleExpression( falseExprs, defaultType: ifTrue?.Type ?? typeof( void ) );

                var type = ifTrue?.Type ?? ifFalse?.Type ?? typeof( void );

                return Condition( test, ifTrue!, ifFalse!, type );
            } );

        return parser;
    }

    private Parser<Expression> LoopParser( Deferred<Expression> statement )
    {
        var parser = Terms.Text( "loop" )
            .Then( _ =>
            {
                var breakLabel = Label( typeof( void ), "Break" );
                var continueLabel = Label( typeof( void ), "Continue" );

                Scope.Push( new Frame( breakLabel, continueLabel ) );

                return (breakLabel, continueLabel);
            } )
            .And(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( statement ),
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( parts =>
            {
                var (breakLabel, continueLabel) = parts.Item1;
                var exprs = parts.Item2;

                try
                {
                    var body = Block( exprs );
                    return Loop( body, breakLabel, continueLabel );
                }
                finally
                {
                    Scope.Pop();
                }
            } );

        return parser;
    }

    private Parser<Expression> SwitchParser( Deferred<Expression> expression, Deferred<Expression> statement )
    {
        var caseUntil = Literals.WhiteSpace( includeNewLines: true )
            .And(
                Terms.Text( "case" )
                .Or( Terms.Text( "default" ) )
                .Or( Terms.Text( "}" )
            ) );

        var caseParser = Terms.Text( "case" )
            .SkipAnd( expression )
            .AndSkip( Terms.Char( ':' ) )
            .And( XsParsers.ZeroOrManyUntil( statement, caseUntil ) )
            .Then( parts =>
            {
                var (testExpression, statements) = parts;
                var body = ConvertToSingleExpression( statements );

                return SwitchCase( body, testExpression );
            } );

        var defaultParser = Terms.Text( "default" )
            .SkipAnd( Terms.Char( ':' ) )
            .SkipAnd( ZeroOrMany( statement ) )
            .Then( statements =>
            {
                var body = ConvertToSingleExpression( statements );
                return body;
            } );

        var parser = Terms.Text( "switch" )
            .Then( _ =>
            {
                var breakLabel = Label( typeof( void ), "Break" );
                Scope.Push( new Frame( breakLabel ) );

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
                    ZeroOrMany( caseParser ).And( ZeroOrOne( defaultParser ) ),
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( parts =>
            {
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
                    Scope.Pop();
                }
            } );

        return parser;
    }

    private Parser<Expression> TryCatchParser( Deferred<Expression> statement )
    {
        var parser = Terms.Text( "try" )
            .SkipAnd(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( statement ),
                    Terms.Char( '}' )
                ).Then( Block )
            )
            .And(
                ZeroOrMany(
                    Terms.Text( "catch" )
                        .SkipAnd(
                            Between(
                                Terms.Char( '(' ),
                                Terms.Identifier().And( ZeroOrOne( Terms.Identifier() ) ), //BF ME discuss - need to test optional identifier
                                Terms.Char( ')' )
                            )
                            .Then( parts =>
                            {
                                var (typeName, variableName) = parts;
                                var exceptionType = Type.GetType( typeName.ToString()! ) ?? typeof( Exception ); //BF ME discuss - type resolution
                                var exceptionVariable = parts.Item2 != null ? Parameter( exceptionType, variableName.ToString() ) : null;

                                return exceptionVariable;
                            }
                        )
                        .And(
                            Between(
                                Terms.Char( '{' ),
                                ZeroOrMany( statement ),
                                Terms.Char( '}' )
                            )
                        )
                        .Then( parts =>
                        {
                            var (exceptionVariable, body) = parts;
                            return Catch( exceptionVariable, Block( body ) );
                        } )
                    )
                )
            )
            .And(
                ZeroOrOne(
                    Terms.Text( "finally" )
                        .SkipAnd(
                            Between(
                                Terms.Char( '{' ),
                                ZeroOrMany( statement ),
                                Terms.Char( '}' )
                            )
                        )
                        .Then( Block )
                    )
            )
            .Then<Expression>( parts =>
            {
                var (tryBlock, catchBlocks, finallyBlock) = parts;
                return TryCatchFinally( tryBlock, finallyBlock, catchBlocks.ToArray() );
            } );

        return parser;
    }

    private Parser<Expression> MethodCallParser( Deferred<Expression> expression, Parser<Expression> identifier )
    {
        var arguments = Separated( Terms.Char( ',' ), expression )
            .Then( parts => parts ?? Array.Empty<Expression>() );

        var parser = identifier
            .And(
                Between(
                    Terms.Char( '(' ),
                    arguments,
                    Terms.Char( ')' )
                )
            )
            .Then<Expression>( parts =>
            {
                var targetExpression = parts.Item1 as ParameterExpression;
                var methodArguments = parts.Item2;
                var methodName = targetExpression!.Name!;

                if ( !_methodTable.TryGetValue( methodName, out var methodInfo ) )
                    throw new Exception( $"Method '{methodName}' not found." );

                return methodInfo.IsStatic
                    ? Call( methodInfo, methodArguments.ToArray() )
                    : Call( targetExpression, methodInfo, methodArguments.ToArray() );
            } );

        return parser;
    }

    private Parser<Expression> LambdaInvokeParser( Deferred<Expression> expression, Parser<Expression> identifier )
    {
        var arguments = Separated( Terms.Char( ',' ), expression )
            .Then( parts => parts ?? Array.Empty<Expression>() );

        var parser = identifier
            .And(
                Between(
                    Terms.Char( '(' ),
                    arguments,
                    Terms.Char( ')' )
                )
            )
            .Then<Expression>( parts =>
            {
                var lambdaExpression = parts.Item1;
                var invocationArguments = parts.Item2; // Arguments 

                return Invoke(
                    lambdaExpression,
                    invocationArguments
                );
            } );

        return parser;
    }
}

internal class Scope
{
    private readonly Stack<Frame> _frames = new();

    public LinkedDictionary<string, ParameterExpression> Variables = new();
    public Frame Frame => _frames.Peek();

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Push( Frame frame )
    {
        _frames.Push( frame );
        Variables.Push();
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Pop()
    {
        _frames.Pop();
        Variables.Pop();
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public ParameterExpression LookupVariable( Parlot.TextSpan ident )
    {
        if ( !Variables.TryGetValue( ident.ToString()!, out var variable ) )
            throw new Exception( $"Variable '{ident}' not found." );

        return variable;
    }
}

internal class Frame
{
    public LabelTarget BreakLabel { get; }
    public LabelTarget ContinueLabel { get; }
    public Dictionary<string, LabelTarget> Labels { get; } = new();

    public Frame( LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        BreakLabel = breakLabel;
        ContinueLabel = continueLabel;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public LabelTarget GetOrCreateLabel( string labelName )
    {
        if ( Labels.TryGetValue( labelName, out var label ) )
            return label;

        label = Label( labelName );
        Labels[labelName] = label;

        return label;
    }
}
