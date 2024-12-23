using System.Linq.Expressions;
using System.Reflection;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.ExpressionScript.Parser;

public interface IParserExtension
{
    void Extend( Parser<Expression> parser );
}

//public class ExpressionScriptParser
//{
//    public static readonly Parser<BinaryExpression> Script;

//    static ExpressionScriptParser()
//    {
//        var identifier = Terms.Identifier();
//        var integerLiteral = Terms.Integer();

//        // Primary expressions

//        var primary = identifier.Then( name => Parameter( typeof(long), name.ToString() ) );

//        // Variable declarations

//        var variableDeclaration = Terms.Text( "let" ).SkipAnd( identifier )
//            .AndSkip( Terms.Text( "=" ) )
//            .And( integerLiteral ).Then( parts =>
//            {

//                var variable = Parameter( typeof(long), parts.Item1.ToString() );
//                return Assign( variable, Constant( parts.Item2 ) );
//            } );

//        Script = variableDeclaration;
//    }

//    public static Expression Parse( string script )
//    {
//        if ( Script.TryParse( script, out var result ) )
//        {
//            return result;
//        }

//        return null;
//    }
//}

public class ExpressionScriptParser
{
    private Parser<Expression> _parser;
    //private readonly List<IParserExtension> _extensions = [];
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
        var scanner = new Parlot.Scanner( script );
        var context = new ParseContext( scanner, useNewLines: true );
        return _parser.Parse( context );
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
        var integerLiteral = Terms.Integer().Then<Expression>( value => Constant( value ) );
        var floatLiteral = Terms.Decimal( NumberOptions.AllowLeadingSign ).Then<Expression>( value => Constant( value ) );
        var stringLiteral = Terms.String().Then<Expression>( value => Constant( value ) );
        var booleanLiteral = Terms.Text( "true" ).Or( Terms.Text( "false" ) ).Then<Expression>( value => Constant( bool.Parse( value ) ) );
        var nullLiteral = Terms.Text( "null" ).Then<Expression>( _ => Constant( null ) );

        var literal = OneOf( integerLiteral, floatLiteral, stringLiteral, booleanLiteral, nullLiteral );

        // Identifiers
        var identifier = Terms.Identifier()
            .Then<Expression>( name => Parameter( typeof( object ), name.ToString() ) );

        // Unary Expressions
        var unaryOperators = Terms.Text( "!" ).Or( Terms.Text( "-" ) );
        
        var unaryExpression = unaryOperators
            .And( expression )
            .Then<Expression>( parts =>
            parts.Item1 switch
            {
                "!" => Not( parts.Item2 ),
                "-" => Negate( parts.Item2 ),
                _ => throw new Exception( "Invalid unary operator." )
            } );

        // Binary Expressions
        var binaryOperators = Terms.Text( "+" )
            .Or( Terms.Text( "-" ) )
            .Or( Terms.Text( "*" ) )
            .Or( Terms.Text( "/" ) )
            .Or( Terms.Text( "%" ) )
            .Or( Terms.Text( "&&" ) )
            .Or( Terms.Text( "||" ) )
            .Or( Terms.Text( "==" ) )
            .Or( Terms.Text( "!=" ) )
            .Or( Terms.Text( "<" ) )
            .Or( Terms.Text( ">" ) )
            .Or( Terms.Text( "<=" ) )
            .Or( Terms.Text( ">=" ) )
            .Or( Terms.Text( "??" ) );

        var binaryExpression = Separated( binaryOperators, expression )
            .Then( parts =>
            {
                var expr = parts[0];
                for ( var i = 1; i < parts.Count; i += 2 )
                {
                    var op = parts[i].ToString();
                    var right = parts[i + 1];

                    expr = op switch
                    {
                        "+" => Add( expr, right ),
                        "-" => Subtract( expr, right ),
                        "*" => Multiply( expr, right ),
                        "/" => Divide( expr, right ),
                        "%" => Modulo( expr, right ),
                        "&&" => AndAlso( expr, right ),
                        "||" => OrElse( expr, right ),
                        "==" => Equal( expr, right ),
                        "!=" => NotEqual( expr, right ),
                        "<" => LessThan( expr, right ),
                        ">" => GreaterThan( expr, right ),
                        "<=" => LessThanOrEqual( expr, right ),
                        ">=" => GreaterThanOrEqual( expr, right ),
                        "??" => Coalesce( expr, right ),
                        _ => throw new InvalidOperationException( $"Unsupported operator: {op}" )
                    };
                }

                return expr;
            } );

        // Postfix Expressions
        var postfixOperators = Terms.Text( "++" ).Or( Terms.Text( "--" ) );

        var postfixExpression = identifier
            .And( postfixOperators )
            .Then<Expression>( parts =>
            parts.Item2 switch
            {
                "++" => PostIncrementAssign( parts.Item1 ),
                "--" => PostDecrementAssign( parts.Item1 ),
                _ => throw new Exception( "Invalid postfix operator." )
            } );

        // Grouped Expressions
        var groupedExpression = Between( Terms.Char( '(' ), expression, Terms.Char( ')' ) );

        // Combine Parsers into Expression
        expression.Parser = literal
            .Or( postfixExpression )
            .Or( groupedExpression )
            .Or( unaryExpression )
            .Or( binaryExpression )
            .Or( identifier );

        // Variable Declarations
        var varDeclaration = Terms.Text( "var" ) //BF var is declaration plus assignment. we need to also support assignment without var.
            .SkipAnd( identifier )
            .AndSkip( Terms.Text( "=" ) )
            .And( expression )
            .Then<Expression>( parts => Assign( parts.Item1, parts.Item2 ) );

        // Assignments
        var assignmentOperators = Terms.Text( "=" )
            .Or( Terms.Text( "+=" ) )
            .Or( Terms.Text( "-=" ) )
            .Or( Terms.Text( "*=" ) )
            .Or( Terms.Text( "/=" ) );

        var assignment = identifier
            .And( assignmentOperators )
            .And( expression )
            .Then<Expression>( parts =>
            {
                var left = parts.Item1;
                var op = parts.Item2;
                var right = parts.Item3;
                return op switch
                {
                    "=" => Assign( left, right ),
                    "+=" => AddAssign( left, right ),
                    "-=" => SubtractAssign( left, right ),
                    "*=" => MultiplyAssign( left, right ),
                    "/=" => DivideAssign( left, right ),
                    _ => throw new InvalidOperationException( $"Unsupported operator: {op}." )
                };
            } );

        // Statements

        var conditionalStatement = ConditionalParser( expression );
        var loopStatement = LoopParser( expression, out var breakStatement, out var continueStatement );
        var switchStatement = SwitchParser( expression );
        var tryCatchStatement = TryCatchParser( expression, identifier );
        var methodCall = MethodCallParser( expression, identifier );
        var lambdaInvocation = LambdaInvokeParser( expression, identifier );

        var statement = varDeclaration
            .Or( assignment )
            .Or( conditionalStatement )
            .Or( loopStatement )
            .Or( switchStatement )
            .Or( breakStatement )
            .Or( continueStatement )
            .Or( methodCall )
            .Or( lambdaInvocation )
            .Or( tryCatchStatement )
            .Or( expression );

        // Script Parsing
        _parser = ZeroOrMany( statement.AndSkip( Terms.Char( ';' ) ) ).Then<Expression>( Block );
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
                    ZeroOrMany( expression ),
                    Terms.Char( '}' )
                )
            )
            .And(
                Terms.Text( "else" )
                    .SkipAnd(
                        Between(
                            Terms.Char( '{' ),
                            ZeroOrMany( expression ),
                            Terms.Char( '}' )
                        )
                    )
                    .Then( Expression ( elseBlock ) => Block( elseBlock ) )
            )
            .Then<Expression>( parts =>
            {
                var condition = parts.Item1;
                var ifTrue = Block( parts.Item2 );
                var ifFalse = parts.Item3 ?? Default( typeof(void) );

                return Condition( condition, ifTrue, ifFalse );
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
            .And(
                Between(
                    Terms.Char( '{' ),
                    ZeroOrMany( expression ),
                    Terms.Char( '}' )
                )
            )
            .Then<Expression>( parts =>
            {
                var breakLabel = Label( typeof(void), "Break" );
                var continueLabel = Label( typeof(void), "Continue" );
                _loopContexts.Push( new LoopContext( breakLabel, continueLabel ) );
                try
                {
                    var body = Block( parts.Item2 );
                    return Loop( body, breakLabel, continueLabel );
                }
                finally
                {
                    _loopContexts.Pop();
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
                return Default( typeof(void) ); //BF TODO Placeholder
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
                                        var exceptionType = Type.GetType( parts.Item1.ToString() ) ?? typeof(Exception); //BF we probably need a resolver here to resolve the type
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

