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
    private readonly Parser<Expression> _xs;
    private readonly Dictionary<string, MethodInfo> _methodTable;
    private readonly List<IParserExtension> _extensions = [];

    private Scope Scope { get; } = new();

    public XsParser( Dictionary<string, MethodInfo> methodTable = null )
    {
        _methodTable = methodTable ?? new Dictionary<string, MethodInfo>();
        _xs = CreateParser();
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
    // Add Throw
    // Add Indexer access

    private Parser<Expression> CreateParser()
    {
        // Expressions

        var expression = ExpressionParser();

        // Statements

        var statement = Deferred<Expression>();

        var conditionalStatement = ConditionalParser( expression, statement );
        var loopStatement = LoopParser( statement );
        var tryCatchStatement = TryCatchParser( statement );
        var switchStatement = SwitchParser( expression, statement );

        var declaration = DeclarationParser( expression );
        var assignment = AssignmentParser( expression );

        var breakStatement = BreakParser();
        var continueStatement = ContinueParser();
        var gotoStatement = GotoParser();
        var labelStatement = LabelParser();
        var returnStatement = ReturnParser( expression );

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
            returnStatement,
            //methodCall
            //lambdaInvocation
            declaration, // must come after statements
            assignment,
            expression
        ).AndSkip( Terms.Char( ';' ) );

        statement.Parser = OneOf(
            complexStatement,
            labelStatement,
            expressionStatement
        );

        // Finalize

        return Between(
                Always().Then<Expression>( _ =>
                {
                    Scope.Push( FrameType.Method );
                    return default;
                } ),
                ZeroOrMany( statement ).Then( statements => 
                    ConvertToFinalExpression( statements, Scope ) 
                ),
                Always<Expression>().Then<Expression>( _ =>
                {
                    Scope.Pop();
                    return default;
                } )
            );
    }

    // Helpers

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

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Expression ConvertToFinalExpression( IReadOnlyList<Expression> expressions, Scope scope )
    {
        var returnLabel = scope.Frame.ReturnLabel;
        var finalType = expressions.Count > 0 ? expressions[^1].Type : null;

        return returnLabel switch
        {
            null => Block( scope.Variables.EnumerateValues(), expressions ),

            _ when returnLabel.Type != finalType
                => throw new InvalidOperationException( $"Mismatched return types: Expected {returnLabel.Type}, found {finalType}." ),

            _ => Block(
                scope.Variables.EnumerateValues(),
                expressions.Concat( [Label( returnLabel, Default( returnLabel.Type ) )] )
            )
        };
    }

    // Expression Parser

    private Parser<Expression> ExpressionParser()
    {
        var expression = Deferred<Expression>();

        // Literals

        var integerLiteral = Terms.Number<int>( NumberOptions.AllowLeadingSign ).Then<Expression>( static value => Constant( value ) );
        var longLiteral = Terms.Number<long>( NumberOptions.AllowLeadingSign ).Then<Expression>( static value => Constant( value ) );
        var floatLiteral = Terms.Number<float>( NumberOptions.Float ).Then<Expression>( static value => Constant( value ) );
        var doubleLiteral = Terms.Number<double>( NumberOptions.Float ).Then<Expression>( static value => Constant( value ) );

        var stringLiteral = Terms.String().Then<Expression>( static value => Constant( value.ToString() ) );
        var booleanLiteral = Terms.Text( "true" ).Or( Terms.Text( "false" ) ).Then<Expression>( static value => Constant( bool.Parse( value ) ) );
        var nullLiteral = Terms.Text( "null" ).Then<Expression>( static _ => Constant( null ) );

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

        return expression.Parser = OneOf(
            binaryExpression
        );
    }

    // Variable Parsers

    private Parser<Expression> AssignmentParser( Parser<Expression> expression )
    {
        return Terms.Identifier()
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
            );
    }

    private Parser<Expression> DeclarationParser( Parser<Expression> expression )
    {
        return Terms.Text( "var" )
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
            );
    }


    // Statement Parsers

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

    private Parser<Expression> ReturnParser( Parser<Expression> expression )
    {
        return Terms.Text( "return" )
            .SkipAnd( ZeroOrOne( expression ) )
            .Then<Expression>( returnValue =>
            {
                var returnType = returnValue?.Type ?? typeof(void);
                var returnLabel = Scope.Frame.GetOrCreateReturnLabel( returnType );

                return returnType == typeof(void)
                    ? Return( returnLabel )
                    : Return( returnLabel, returnValue, returnType );
            } );
    }

    private Parser<Expression> ConditionalParser( Parser<Expression> expression, Deferred<Expression> statement )
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
            .Then<Expression>( static parts =>
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

                Scope.Push( FrameType.Child, breakLabel, continueLabel );

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

    private Parser<Expression> SwitchParser( Parser<Expression> expression, Deferred<Expression> statement )
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
            .Then( static parts =>
            {
                var (testExpression, statements) = parts;
                var body = ConvertToSingleExpression( statements );

                return SwitchCase( body, testExpression );
            } );

        var defaultParser = Terms.Text( "default" )
            .SkipAnd( Terms.Char( ':' ) )
            .SkipAnd( ZeroOrMany( statement ) )
            .Then( static statements =>
            {
                var body = ConvertToSingleExpression( statements );
                return body;
            } );

        var parser = Terms.Text( "switch" )
            .Then( _ =>
            {
                var breakLabel = Label( typeof( void ), "Break" );
                Scope.Push( FrameType.Child, breakLabel );

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
                            .Then( static parts =>
                            {
                                var (typeName, variableName) = parts;
                                var exceptionType = Type.GetType( typeName.ToString()! ) ?? typeof( Exception ); //BF ME discuss - type resolution
                                var exceptionVariable = variableName != null ? Parameter( exceptionType, variableName.ToString() ) : null;

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
                        .Then( static parts =>
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
            .Then<Expression>( static parts =>
            {
                var (tryBlock, catchBlocks, finallyBlock) = parts;
                return TryCatchFinally( tryBlock, finallyBlock, catchBlocks.ToArray() );
            } );

        return parser;
    }

    private Parser<Expression> MethodCallParser( Parser<Expression> expression, Parser<Expression> identifier )
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

    private Parser<Expression> LambdaInvokeParser( Parser<Expression> expression, Parser<Expression> identifier )
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
            .Then<Expression>( static parts =>
            {
                var (lambdaExpression, invocationArguments) = parts;

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
    public void Push( FrameType frameType, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        var parent = _frames.Count > 0 ? _frames.Peek() : null;
        var frame = new Frame( frameType, parent, breakLabel, continueLabel );

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

internal enum FrameType
{
    Method,
    Child
}

internal class Frame
{
    public FrameType FrameType { get; }
    public Frame Parent { get; }

    public LabelTarget BreakLabel { get; }
    public LabelTarget ContinueLabel { get; }
    public LabelTarget ReturnLabel { get; private set; }

    public Dictionary<string, LabelTarget> Labels { get; } = new();

    public Frame( FrameType frameType, Frame parent = null, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        FrameType = frameType;
        Parent = parent;
        BreakLabel = breakLabel;
        ContinueLabel = continueLabel;
    }

    public LabelTarget GetOrCreateLabel( string labelName )
    {
        if ( Labels.TryGetValue( labelName, out var label ) )
            return label;

        label = Label( labelName );
        Labels[labelName] = label;

        return label;
    }

    public LabelTarget GetOrCreateReturnLabel( Type returnType )
    {
        var currentFrame = this;

        while ( currentFrame != null )
        {
            if ( currentFrame.FrameType == FrameType.Method )
            {
                if ( currentFrame.ReturnLabel == null )
                {
                    currentFrame.ReturnLabel = Label( returnType, "ReturnLabel" );
                }
                else if ( currentFrame.ReturnLabel.Type != returnType )
                {
                    throw new InvalidOperationException(
                        $"Mismatched return types: Expected {currentFrame.ReturnLabel.Type}, found {returnType}." );
                }

                return currentFrame.ReturnLabel;
            }

            currentFrame = currentFrame.Parent;
        }

        throw new InvalidOperationException( "No enclosing method frame to handle return." );
    }
}
