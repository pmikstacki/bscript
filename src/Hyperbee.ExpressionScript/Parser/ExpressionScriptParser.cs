using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hyperbee.Collections;
using Hyperbee.Collections.Extensions;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.Parser;

public interface IParserExtension
{
    void Extend( Parser<Expression> parser );
}

public class ExpressionScriptParser
{
    private Parser<Expression> _xs;

    //private readonly List<IParserExtension> _extensions = []; // TODO: Add expression extensions

    private readonly LinkedDictionary<string, ParameterExpression> _variableTable = new(); // TODO: push and pop scopes

    private readonly Dictionary<string, MethodInfo> _methodTable;
    private readonly Stack<LoopContext> _loopContexts = new();

    public ExpressionScriptParser( Dictionary<string, MethodInfo> methodTable = null )
    {
        _methodTable = methodTable ?? new Dictionary<string, MethodInfo>();
        InitializeParser();
    }

    public void AddExtension( IParserExtension extension )
    {
        //_extensions.Add( extension );
    }

    public Expression Parse( string script )
    {
        using ( _variableTable.Enter() )
        {
            var scanner = new Parlot.Scanner( script );
            var context = new ParseContext( scanner );

            return _xs.Parse( context );
        }
    }

    // Add Goto
    // Add Return
    // Add ?? and ??= operators
    // Add Extensions

    private void InitializeParser()
    {
        // Deferred parser for recursive expressions
        var expression = Deferred<Expression>();

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

        var prefixedIdentifier = OneOf( Terms.Text( "++" ), Terms.Text( "--" ) )
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

        var postfixedIdentifier = Terms.Identifier()
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
            prefixedIdentifier.Named( "prefix-identifier" ),
            postfixedIdentifier.Named( "postfix-identifier" ),
            primaryIdentifier.Named( "primary-identifier" )
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

        var conditionalStatement = ConditionalParser( expression ).Named( "conditional" );
        var loopStatement = LoopParser( expression, out var breakStatement, out var continueStatement );

        //var switchStatement = SwitchParser( expression );
        //var tryCatchStatement = TryCatchParser( expression, identifier );
        //var methodCall = MethodCallParser( expression, identifier );
        //var lambdaInvocation = LambdaInvokeParser( expression, identifier );

        var complexStatement = OneOf( // Complex statements are statements that contain other statements
            conditionalStatement,
            loopStatement

        //switchStatement
        //tryCatchStatement
        ).Named( "complex-statement" );

        var simpleStatement = OneOf( // Simple statements are statements that can be terminated with a semicolon
            breakStatement,
            continueStatement,
            //methodCall
            //lambdaInvocation
            declaration,
            assignment
        ).AndSkip( Terms.Char( ';' ) ).Named( "simple-statement" );

        var statement = OneOf( complexStatement, simpleStatement ).Named( "statement" );

        // Finalize

        expression.Parser = OneOf( statement, binaryExpression );

        _xs = ZeroOrMany( expression )
            .Then<Expression>( expressions => Block(
                _variableTable.EnumerateValues(),
                expressions
            ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private ParameterExpression LookupVariable( Parlot.TextSpan ident )
    {
        if ( !_variableTable.TryGetValue( ident.ToString()!, out var variable ) )
            throw new Exception( $"Variable '{ident}' not found." );

        return variable;
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

    private Parser<Expression> ConditionalParser( Deferred<Expression> expression )
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
                    ZeroOrMany( expression.AndSkip( Terms.Char( ';' ) ) ), //BF don't understand the need for ';' here
                    Terms.Char( '}' )
                )
            )
            .And(
                Terms.Text( "else" )
                    .SkipAnd(
                        Between(
                            Terms.Char( '{' ),
                            ZeroOrMany( expression.AndSkip( Terms.Char( ';' ) ) ), //BF don't understand the need for ';' here
                            Terms.Char( '}' )
                        )
                    )
            )
            .Then<Expression>( parts =>
            {
                var (test, trueExprs, falseExprs) = parts;

                var ifTrue = trueExprs.Count > 1
                    ? Block( trueExprs )
                    : trueExprs[0];

                var ifFalse = falseExprs.Count > 1
                    ? Block( falseExprs )
                    : falseExprs[0];

                var type = ifTrue?.Type ?? ifFalse?.Type ?? typeof( void );

                return Condition( test, ifTrue!, ifFalse!, type );
            } );

        return parser;
    }

    private Parser<Expression> LoopParser( Deferred<Expression> expression, out Parser<Expression> breakStatement, out Parser<Expression> continueStatement )
    {
        // Break and Continue
        breakStatement = Terms.Text( "break" )
            .Then<Expression>( _ =>
            {
                if ( _loopContexts.Count == 0 )
                    throw new Exception( "Invalid use of 'break' outside of a loop." );

                return Break( _loopContexts.Peek().BreakLabel );
            } );

        continueStatement = Terms.Text( "continue" )
            .Then<Expression>( _ =>
            {
                if ( _loopContexts.Count == 0 )
                    throw new Exception( "Invalid use of 'continue' outside of a loop." );

                return Continue( _loopContexts.Peek().ContinueLabel );
            } );

        // Loops
        var parser = Terms.Text( "loop" )
            .Then( _ =>
            {
                var breakLabel = Label( typeof( void ), "Break" );
                var continueLabel = Label( typeof( void ), "Continue" );

                _loopContexts.Push( new LoopContext( breakLabel, continueLabel ) );

                return (breakLabel, continueLabel);
            } )
            .And(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( expression.AndSkip( Terms.Char( ';' ) ) ), //BF don't understand the need for ';' here
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( parts =>
            {
                var (breakLabel, continueLabel) = parts.Item1; //BF not being hit when 'break;' is present
                var exprs = parts.Item2;

                try
                {
                    var body = Block( exprs );
                    return Loop( body, breakLabel, continueLabel );
                }
                finally
                {
                    _loopContexts.Pop(); // Ensure context is removed after parsing
                }
            } );

        return parser;
    }

    private Parser<Expression> SwitchParser( Deferred<Expression> expression )
    {
        var parser = Terms.Text( "switch" )
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
                    ZeroOrMany( expression ),
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( parts =>
            {
                // Implement switch parsing logic here
                return Default( typeof( void ) ); //BF TODO Placeholder
            } );

        return parser;
    }

    private Parser<Expression> TryCatchParser( Deferred<Expression> expression, Parser<Expression> identifier )
    {
        var parser = Terms.Text( "try" )
            .SkipAnd(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( expression ),
                    Terms.Char( '}' )
                ).Then( Block )
            )
            .And(
                ZeroOrMany(
                    Terms.Text( "catch" )
                        .SkipAnd(
                            Between(
                                Terms.Char( '(' ),
                                // Parse exception type and optional variable name
                                identifier.And(
                                        Terms.Identifier().AndSkip( Terms.Text( " " ) ).Or( null ) // Optional variable name
                                    )
                                    .Then( parts =>
                                    {
                                        // Build the exception parameter (or null if not provided)
                                        var exceptionType = Type.GetType( parts.Item1.ToString() ) ?? typeof( Exception ); //BF we probably need a resolver here to resolve the type
                                        var exceptionVariable = parts.Item2 != null ? Parameter( exceptionType, parts.Item2.ToString() ) : null;
                                        return exceptionVariable;
                                    } )
                                    .Or( null ), // Default to no parameter if the catch has no parentheses
                                Terms.Char( ')' )
                            ).Or( null ) // Handle missing parentheses gracefully
                        )
                        .And(
                            Between(
                                Terms.Char( '{' ),
                                ZeroOrMany( expression ), // Parse the body of the catch block
                                Terms.Char( '}' )
                            )
                        )
                        .Then( parts =>
                        {
                            var exceptionVariable = parts.Item1; // Exception variable (if any)
                            var body = parts.Item2; // Catch block body

                            // Return a CatchBlock
                            return Catch( exceptionVariable, Block( body ) );
                        } )
                )
            )
            .And(
                Terms.Text( "finally" )
                    .SkipAnd(
                        Between(
                            Terms.Char( '{' ),
                            ZeroOrMany( expression ),
                            Terms.Char( '}' )
                        )
                    )
                    .Then( Block )
                    .Or( null ) // Fallback to null if no finally block exists
            )
            .Then<Expression>( parts =>
            {
                var tryBlock = parts.Item1;
                var catchBlocks = parts.Item2.ToArray();
                var finallyBlock = parts.Item3;

                return TryCatchFinally( tryBlock, finallyBlock, catchBlocks );
            } );

        return parser;
    }
}

public class LoopContext
{
    public LabelTarget BreakLabel { get; }
    public LabelTarget ContinueLabel { get; }

    public LoopContext( LabelTarget breakLabel, LabelTarget continueLabel )
    {
        BreakLabel = breakLabel;
        ContinueLabel = continueLabel;
    }
}

