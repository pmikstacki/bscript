using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS;

// Parser TODO
//
// Add Method calls and chaining
// Add Member access
// Add Indexer access
// Add Array access
// Add Async Await

public class XsParser
{
    private readonly Parser<Expression> _xs;
    private readonly List<IParseExtension> _extensions = [];

    private TypeResolver Resolver { get; } = new();
    private ParseScope Scope { get; } = new();

    public IReadOnlyCollection<IParseExtension> Extensions
    {
        get => _extensions;
        init => _extensions.AddRange( value );
    }

    public IReadOnlyCollection<Assembly> References
    {
        get => Resolver.References;
        init => Resolver.AddReferences( value );
    }

    public XsParser()
    {
        _xs = CreateParser();
    }

    public Expression Parse( string script )
    {
        var scanner = new Scanner( script );
        var context = new ParseContext( scanner ) { WhiteSpaceParser = XsParsers.WhitespaceOrNewLineOrComment() };

        //context.OnEnterParser = ( obj, p ) => { 
        //    Console.WriteLine( "Enter: {0}, {1}", obj, p );
        //};
        //context.OnExitParser = ( obj, p ) => {
        //    Console.WriteLine( "Exit: {0}, {1}", obj, p );
        //};

        return _xs.Parse( context );
    }

    private Parser<Expression> CreateParser()
    {
        var statement = Deferred<Expression>();

        // Expressions

        var expression = ExpressionParser( statement );

        // Statements

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
        var throwStatement = ThrowParser( expression );

        // Compose Statements

        GetExtensionParsers( expression, statement, out var complexExtensions, out var singleExtensions );

        var complexStatement = OneOf(
            conditionalStatement,
            loopStatement,
            tryCatchStatement,
            switchStatement,
            OneOf( complexExtensions )
        );

        var singleLineStatement = OneOf(
            breakStatement,
            continueStatement,
            gotoStatement,
            returnStatement,
            throwStatement,
            OneOf( singleExtensions )
        ).AndSkip( Terms.Char( ';' ) );

        var expressionStatement = OneOf(
            declaration,
            assignment,
            expression
        ).AndSkip( Terms.Char( ';' ) );

        statement.Parser = OneOf(
            complexStatement,
            labelStatement, // colon terminated
            singleLineStatement,
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
        return ConvertToSingleExpression( typeof( void ), expressions );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Expression ConvertToSingleExpression( Type type, IReadOnlyCollection<Expression> expressions )
    {
        type ??= typeof( void );

        return expressions?.Count switch
        {
            null or 0 => Default( type ),
            1 => expressions.First(),
            _ => Block( expressions )
        };
    }

    private static Expression ConvertToFinalExpression( IReadOnlyList<Expression> expressions, ParseScope scope )
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

    private void GetExtensionParsers( Parser<Expression> expression, Deferred<Expression> statement, out Parser<Expression>[] complexExtensions, out Parser<Expression>[] singleExtensions )
    {
        var xsContext = new XsContext( Resolver, Scope, expression, statement );

        complexExtensions = _extensions
            .Where( x => x.Type == ExtensionType.ComplexStatement )
            .Select( x => x.Parser( xsContext ) )
            .ToArray();

        singleExtensions = _extensions
            .Where( x => x.Type == ExtensionType.SingleStatement )
            .Select( x => x.Parser( xsContext ) )
            .ToArray();
    }

    // Expression Parser

    private Parser<Expression> ExpressionParser( Deferred<Expression> statement )
    {
        var expression = Deferred<Expression>();

        // Literals

        var integerLiteral = Terms.Number<int>( NumberOptions.AllowLeadingSign )
            .AndSkip( ZeroOrOne( Terms.Text( "N", caseInsensitive: true ) ) )
            .Then<Expression>( static value => Constant( value ) );

        var longLiteral = Terms.Number<long>( NumberOptions.AllowLeadingSign )
            .AndSkip( Terms.Text( "L", caseInsensitive: true ) )
            .Then<Expression>( static value => Constant( value ) );

        var floatLiteral = Terms.Number<float>( NumberOptions.Float )
            .AndSkip( Terms.Text( "F", caseInsensitive: true ) )
            .Then<Expression>( static value => Constant( value ) );

        var doubleLiteral = Terms.Number<double>( NumberOptions.Float )
            .AndSkip( Terms.Text( "D", caseInsensitive: true ) )
            .Then<Expression>( static value => Constant( value ) );

        var booleanLiteral = Terms.Text( "true" ).Or( Terms.Text( "false" ) )
            .Then<Expression>( static value => Constant( bool.Parse( value ) ) );

        var stringLiteral = Terms.String().Then<Expression>( static value => Constant( value.ToString() ) );
        var nullLiteral = Terms.Text( "null" ).Then<Expression>( static _ => Constant( null ) );

        var literal = OneOf(
            longLiteral,
            doubleLiteral,
            floatLiteral,
            integerLiteral,
            stringLiteral,
            booleanLiteral,
            nullLiteral
        ).Named( "literal" );

        // Identifiers

        var valueIdentifier = XsParsers.ValueIdentifier( Scope.Variables );
        var typeIdentifier = XsParsers.TypeIdentifier( Resolver );

        var identifier = OneOf(
            valueIdentifier,
            typeIdentifier
        ).Named( "identifier" );

        // Grouped Expressions

        var groupedExpression = Between(
            Terms.Char( '(' ),
            expression,
            Terms.Char( ')' )
        ).Named( "group" );

        // Primary Expressions

        var primaryExpression = Deferred<Expression>();

        var methodCall = MethodCallParser( identifier, primaryExpression );
        var lambdaInvocation = LambdaInvokeParser( primaryExpression );
        var property = PropertyParser( identifier, primaryExpression );

        primaryExpression.Parser = OneOf(
            methodCall,
            lambdaInvocation,
            property,
            literal,
            identifier,
            groupedExpression
        ).Named( "primary" );

        // Prefix and Postfix Expressions

        var prefixExpression = OneOf(
                Terms.Text( "++" ),
                Terms.Text( "--" )
            )
            .And( primaryExpression )
            .Then<Expression>( parts =>
            {
                var (op, variable) = parts;

                return op switch
                {
                    "++" => PreIncrementAssign( variable ),
                    "--" => PreDecrementAssign( variable ),
                    _ => throw new InvalidOperationException( $"Unsupported prefix operator: {op}." )
                };
            } );

        var postfixExpression = primaryExpression
            .And(
                OneOf(
                    Terms.Text( "++" ),
                    Terms.Text( "--" )
                )
            )
            .Then<Expression>( parts =>
            {
                var (variable, op) = parts;

                return op switch
                {
                    "++" => PostIncrementAssign( variable ),
                    "--" => PostDecrementAssign( variable ),
                    _ => throw new InvalidOperationException( $"Unsupported postfix operator: {op}." )
                };
            } );


        // Unary Expressions

        var unaryExpression = OneOf(
            prefixExpression,
            postfixExpression,
            primaryExpression
        ).Unary(
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

        // Other Expressions

        var newExpression = NewParser( expression );
        var lambdaExpression = LambdaParser( primaryExpression, statement );

        return expression.Parser = OneOf(
            newExpression,
            lambdaExpression,
            binaryExpression
        );
    }

    // Helper Parsers

    private static Parser<IReadOnlyList<Expression>> Arguments( Parser<Expression> expression )
    {
        return ZeroOrOne( Separated( Terms.Char( ',' ), expression ) )
            .Then( arguments => arguments ?? Array.Empty<Expression>() );
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
                var returnType = returnValue?.Type ?? typeof( void );
                var returnLabel = Scope.Frame.GetOrCreateReturnLabel( returnType );

                return returnType == typeof( void )
                    ? Return( returnLabel )
                    : Return( returnLabel, returnValue, returnType );
            } );
    }

    private Parser<Expression> ThrowParser( Parser<Expression> expression )
    {
        return Terms.Text( "throw" )
            .SkipAnd( ZeroOrOne( expression ) )
            .Then<Expression>( exceptionExpression =>
            {
                if ( exceptionExpression != null && !typeof( Exception ).IsAssignableFrom( exceptionExpression.Type ) )
                {
                    throw new InvalidOperationException(
                        $"Invalid throw argument: Expected an exception type, but found {exceptionExpression.Type}." );
                }

                return Throw( exceptionExpression );
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
                var ifFalse = ConvertToSingleExpression( ifTrue?.Type, falseExprs );

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

    private Parser<Expression> LambdaParser( Parser<Expression> expression, Deferred<Expression> statement )
    {
        var parameters = ZeroOrOne(
                Separated(
                    Terms.Char( ',' ),
                    Terms.Identifier().And( Terms.Identifier() )  // TODO: Add identifier
                )
            )
            .Then( ( ctx, parts ) =>
            {
                if ( parts == null )
                    return [];

                Scope.Push( FrameType.Method );

                return parts.Select( p =>
                {
                    var (typeName, name) = p;

                    var type = Resolver.ResolveType( typeName.ToString() )
                        ?? throw new InvalidOperationException( $"Unknown type: {typeName}." );

                    var parameter = Parameter( type, name.ToString() );

                    Scope.Variables.Add( name.ToString()!, parameter );

                    return parameter;

                } ).ToArray();
            } );

        var parser =
            Between(
                Terms.Char( '(' ),
                parameters,
                Terms.Char( ')' ) )
            .AndSkip( Terms.Text( "=>" ) )
            .And(
                OneOf(
                    XsParsers.ListOfOne( expression ),
                    Between(
                        Terms.Char( '{' ),
                        ZeroOrMany( statement ),
                        Terms.Char( '}' )
                    )
                )
            )
            .Then<Expression>( parts =>
            {
                var (parameters, body) = parts;

                var type = body.Count == 0 ? typeof( void ) : body[^1].Type;

                if ( parameters.Length == 0 )
                {
                    return Lambda( ConvertToSingleExpression( type, body ) );
                }
                else
                {
                    try
                    {
                        return Lambda( ConvertToSingleExpression( type, body ), parameters );
                    }
                    finally
                    {
                        Scope.Pop();
                    }
                }



            } ).Named( "Lambda" );

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
                )
            )
            .And(
                ZeroOrMany(
                    Terms.Text( "catch" )
                        .SkipAnd(
                            Between(
                                Terms.Char( '(' ),
                                Terms.Identifier().And( ZeroOrOne( Terms.Identifier() ) ),
                                Terms.Char( ')' )
                            )
                            .Then( parts =>
                            {
                                var (typeName, variableName) = parts;
                                var type = Resolver.ResolveType( typeName.ToString()! );

                                if ( type == null )
                                    throw new InvalidOperationException( $"Unknown type: {typeName}." );

                                var name = variableName.Length == 0 ? null : variableName.ToString();

                                return Parameter( type, name );
                            }
                        )
                        .And(
                            Between(
                                Terms.Char( '{' ),
                                ZeroOrMany( statement ),
                                Terms.Char( '}' )
                            )
                        )
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
                    )
            )
            .Then<Expression>( static parts =>
            {
                var (tryParts, catchParts, finallyParts) = parts;

                var tryType = tryParts?[^1].Type ?? typeof( void );

                var tryBlock = ConvertToSingleExpression( tryType, tryParts );
                var finallyBlock = ConvertToSingleExpression( finallyParts );

                var catchBlocks = catchParts.Select( part =>
                {
                    var (exceptionVariable, catchBody) = part;
                    return Catch( exceptionVariable, Block( tryType, catchBody ) );
                } ).ToArray();

                return TryCatchFinally( tryBlock, finallyBlock, catchBlocks );
            } );

        return parser;
    }

    private Parser<Expression> NewParser( Parser<Expression> expression )
    {
        var typeNameParser = Separated( Terms.Char( '.' ), Terms.Identifier() )
            .Then( parts =>
            {
                var typeName = string.Join( ".", parts );
                var type = Resolver.ResolveType( typeName );

                if ( type == null )
                    throw new InvalidOperationException( $"Unknown type: {typeName}." );

                return type;
            } );

        var parser = Terms.Text( "new" )
            .SkipAnd( typeNameParser )
            .And(
                Between(
                    Terms.Char( '(' ),
                    Arguments( expression ),
                    Terms.Char( ')' )
                )
            )
            .Then<Expression>( parts =>
            {
                var (type, arguments) = parts;

                var constructor = type.GetConstructor( arguments.Select( arg => arg.Type ).ToArray() );

                if ( constructor == null )
                    throw new InvalidOperationException( $"No matching constructor found for type {type.Name}." );

                return New( constructor, arguments );
            } );

        return parser;
    }

    private Parser<Expression> PropertyParser( Parser<Expression> identifier, Parser<Expression> expression )
    {
        var parser = identifier
            .AndSkip( Terms.Text( "." ) )
            .And( Terms.Identifier() )
            .Then<Expression>( parts =>
            {
                var (targetExpression, propertyName) = parts;

                return targetExpression switch
                {
                    ConstantExpression ce => Property( ce, propertyName.ToString()! ),
                    ParameterExpression pe => Property( pe, pe.Type, propertyName.ToString()! ),
                    _ => throw new InvalidOperationException( "Invalid target expression." ),
                };
            } );

        return parser;
    }

    private Parser<Expression> MethodCallParser( Parser<Expression> identifier, Parser<Expression> expression )
    {
        var parser = identifier
                .AndSkip( Terms.Text( "." ) )
                .And( Terms.Identifier() )
                .And(
                    Between(
                        Terms.Char( '(' ),
                        Arguments( expression ),
                        Terms.Char( ')' )
                    )
            )
            .Then<Expression>( parts =>
            {
                var (targetExpression, methodName, methodArguments) = parts;

                var type = targetExpression switch
                {
                    ConstantExpression ce => (Type) ce.Value,
                    ParameterExpression pe => pe.Type,
                    _ => throw new InvalidOperationException( "Invalid target expression." )
                };

                var methodInfo = TypeResolver.FindMethod( type, methodName.ToString()!, methodArguments );

                if ( methodInfo == null )
                    throw new MissingMethodException( $"Method '{methodName}' not found." );

                return methodInfo.IsStatic
                    ? Call( methodInfo, methodArguments.ToArray() )
                    : Call( targetExpression, methodInfo, methodArguments.ToArray() );
            } );

        return parser;
    }

    private Parser<Expression> LambdaInvokeParser( Parser<Expression> expression )
    {
        var parser = Terms.Identifier()
            .And(
                Between(
                    Terms.Char( '(' ),
                    Arguments( expression ),
                    Terms.Char( ')' )
                )
            )
            .Then<Expression>( parts =>
            {
                var (targetName, invocationArguments) = parts;
                var targetExpression = Scope.LookupVariable( targetName );

                return Invoke(
                    targetExpression,
                    invocationArguments
                );
            } );

        return parser;
    }
}

