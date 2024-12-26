using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hyperbee.Collections;
using Hyperbee.Collections.Extensions;
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
    private readonly LinkedDictionary<string, ParameterExpression> _variableTable = new(); // TODO: push and pop scopes

    private readonly Dictionary<string, MethodInfo> _methodTable;
    private readonly Stack<FlowContext> _flowContexts = new();

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
        using ( _variableTable.Enter() )
        {
            var scanner = new Parlot.Scanner( script );
            var context = new ParseContext( scanner ) { WhiteSpaceParser = XsParsers.WhitespaceOrNewLineOrComment() };

            return _xs.Parse( context );
        }
    }

    // Parser TODO
    //
    // Add LinkedDictionary scopes and validate nesting complex statements and var declarations
    // Add Import statements //BF ME discuss - how should we include and resolve types
    // Add New
    // Add Return
    // Add Throw
    // Add Method calls
    // Add Lambda expressions //BF ME discuss
    // Add Member access
    // Add Indexer access
    // Add Extensions
    // Add Goto
    // Add ?? and ??= operators

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

        var primaryIdentifier = Terms.Identifier().Then<Expression>( LookupVariable );

        var prefixIdentifier = OneOf( Terms.Text( "++" ), Terms.Text( "--" ) )
            .And( Terms.Identifier() )
            .Then<Expression>( parts =>
            {
                var (op, ident) = parts;
                var variable = LookupVariable( ident );

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
                var variable = LookupVariable( ident );

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
                _variableTable.Add( left, variable );

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
                )
            )
            .And( expression )
            .Then<Expression>( parts =>
                {
                    var (ident, op, right) = parts;
                    var left = LookupVariable( ident );

                    return op switch
                    {
                        "=" => Assign( left, right ),
                        "+=" => AddAssign( left, right ),
                        "-=" => SubtractAssign( left, right ),
                        "*=" => MultiplyAssign( left, right ),
                        "/=" => DivideAssign( left, right ),
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
            //methodCall
            //lambdaInvocation
            declaration,
            assignment,
            expression
        ).AndSkip( Terms.Char( ';' ) );

        statement.Parser = OneOf(
            complexStatement,
            expressionStatement
        );

        // Finalize

        _xs = ZeroOrMany( statement )
            .Then<Expression>( statements => Block(
                _variableTable.EnumerateValues(),
                statements
            ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private ParameterExpression LookupVariable( Parlot.TextSpan ident )
    {
        if ( !_variableTable.TryGetValue( ident.ToString()!, out var variable ) )
            throw new Exception( $"Variable '{ident}' not found." );

        return variable;
    }

    private Parser<Expression> BreakParser()
    {
        return Terms.Text( "break" )
            .Then<Expression>( _ =>
            {
                if ( _flowContexts.Count == 0 )
                    throw new Exception( "Invalid use of 'break' outside of a loop or switch." );

                var breakLabel = _flowContexts.Peek().BreakLabel;
                return Break( breakLabel );
            } );
    }

    private Parser<Expression> ContinueParser()
    {
        return Terms.Text( "continue" )
            .Then<Expression>( _ =>
            {
                if ( _flowContexts.Count == 0 )
                    throw new Exception( "Invalid use of 'continue' outside of a loop." );

                var continueLabel = _flowContexts.Peek().ContinueLabel;
                if ( continueLabel == null )
                    throw new Exception( "'continue' is not valid in a switch statement." );

                return Continue( continueLabel );
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

                var ifTrue = trueExprs.Count > 1
                    ? Block( trueExprs )
                    : trueExprs[0];

                var ifFalse = falseExprs switch
                {
                    null => Default( ifTrue?.Type ?? typeof( void ) ),
                    _ => falseExprs.Count > 1
                        ? Block( falseExprs )
                        : falseExprs[0]
                };

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

                _flowContexts.Push( new FlowContext( breakLabel, continueLabel ) );

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
                    _flowContexts.Pop(); // Ensure context is removed after parsing
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

                var body = statements.Count > 1
                    ? Block( statements )
                    : statements.FirstOrDefault() ?? Default( typeof(void) );

                return SwitchCase( body, testExpression );
            } );

        var defaultParser = Terms.Text( "default" )
            .SkipAnd( Terms.Char( ':' ) )
            .SkipAnd( ZeroOrMany( statement ) )
            .Then( statements =>
            {
                var body = statements.Count > 1
                    ? Block( statements )
                    : statements.FirstOrDefault() ?? Default( typeof(void) );

                return body;
            } );

        var parser = Terms.Text( "switch" )
            .Then( _ =>
            {
                var breakLabel = Label( typeof(void), "Break" );
                _flowContexts.Push( new FlowContext( breakLabel ) );

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
                    _flowContexts.Pop();
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
                                var ( typeName, variableName) = parts;
                                var exceptionType = Type.GetType( typeName.ToString()! ) ?? typeof( Exception ); //BF ME discuss - need to resolve type
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
                            var ( exceptionVariable, body ) = parts; 
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
                var ( tryBlock, catchBlocks, finallyBlock) = parts;
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

public class FlowContext
{
    public LabelTarget BreakLabel { get; }
    public LabelTarget ContinueLabel { get; }

    public FlowContext( LabelTarget breakLabel, LabelTarget continueLabel = null )
    {
        BreakLabel = breakLabel;
        ContinueLabel = continueLabel;
    }
}

