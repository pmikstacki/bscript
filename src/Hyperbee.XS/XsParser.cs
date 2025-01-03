using System.Collections.ObjectModel;
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
// Add Indexer access
// Add Array access
// Add Async Await

public class XsContext : ParseContext
{
    public TypeResolver Resolver { get; }
    public ParseScope Scope { get; } = new();

    public XsContext( XsConfig config, Scanner scanner, bool useNewLines = false )
        : base( scanner, useNewLines )
    {
        Resolver = new TypeResolver( config?.References );
    }

    public void Deconstruct( out ParseScope scope, out TypeResolver resolver )
    {
        scope = Scope;
        resolver = Resolver;
    }
}

public class XsConfig
{
    public IReadOnlyCollection<Assembly> References { get; init; } = ReadOnlyCollection<Assembly>.Empty;
    public static IReadOnlyCollection<IParseExtension> Extensions { get; set; } = ReadOnlyCollection<IParseExtension>.Empty;
}

internal static class ParserContextExtensions
{
    public static void Deconstruct( this ParseContext context, out ParseScope scope, out TypeResolver resolver )
    {
        if ( context is XsContext xsContext )
        {
            scope = xsContext.Scope;
            resolver = xsContext.Resolver;
            return;
        }

        scope = default;
        resolver = default;
    }
}

public class XsParser
{
    private static readonly Parser<Expression> __xs = CreateParser();

    public Expression Parse( string script ) => Parse( default, script );

    public Expression Parse( XsConfig config, string script )
    {
        var scanner = new Scanner( script );
        var context = new XsContext( config, scanner ) { WhiteSpaceParser = XsParsers.WhitespaceOrNewLineOrComment() };

        return __xs.Parse( context );
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

    private static void GetExtensionParsers( Parser<Expression> expression, Deferred<Expression> statement, out Parser<Expression>[] complexExtensions, out Parser<Expression>[] singleExtensions )
    {
        complexExtensions = XsConfig.Extensions
            .Where( x => x.Type == ExtensionType.ComplexStatement )
            .Select( x => x.Parser( expression, statement ) )
            .ToArray();

        singleExtensions = XsConfig.Extensions
            .Where( x => x.Type == ExtensionType.SingleStatement )
            .Select( x => x.Parser( expression, statement ) )
            .ToArray();
    }

    // Parsers

    private static Parser<Expression> CreateParser()
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
            Always().Then<Expression>( static ( ctx, _ ) =>
            {
                var (scope, _) = ctx;
                scope.Push( FrameType.Method );
                return default;
            } ),
            ZeroOrMany( statement ).Then( static  (ctx, statements) =>
            {
                var (scope, _) = ctx;
                return ConvertToFinalExpression( statements, scope );
            } ),
            Always<Expression>().Then<Expression>( static ( ctx, _ ) =>
            {
                var (scope, _) = ctx;
                scope.Pop();

                // Ensure we've reached the end of the script
                var cursor = ctx.Scanner.Cursor;
                ctx.SkipWhiteSpace();

                if ( cursor.Eof == false )
                    throw new SyntaxErrorException( "Syntax Error. Failure parsing script.", cursor );

                return default;
            } )
        );
    }

    // Expression Parser

    private static Parser<Expression> ExpressionParser( Deferred<Expression> statement )
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

        var varIdentifier = XsParsers.VariableIdentifier();
        var typeIdentifier = XsParsers.TypeIdentifier();

        var identifier = OneOf(
            varIdentifier,
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

        var newExpression = NewParser( expression );
        var lambdaExpression = LambdaParser( identifier, primaryExpression, statement );

        var baseExpression = OneOf(
            newExpression,
            literal,
            identifier,
            groupedExpression,
            lambdaExpression
        ).Named( "baseExpression" );

        var lambdaInvocation = LambdaInvokeParser( primaryExpression );
        var memberAccess = MemberAccessParser( baseExpression, expression );

        primaryExpression.Parser = OneOf(
            lambdaInvocation,
            memberAccess,
            baseExpression
        ).Named( "primary" );

        var indexerAccess = IndexerAccessParser( primaryExpression, expression );
        var accessorExpression = OneOf(
            indexerAccess,
            primaryExpression
        ).Named( "indexer" ); ;

        // Prefix and Postfix Expressions

        var prefixExpression = OneOf(
                Terms.Text( "++" ),
                Terms.Text( "--" )
            )
            .And( varIdentifier )
            .Then<Expression>( static parts =>
            {
                var (op, variable) = parts;

                return op switch
                {
                    "++" => PreIncrementAssign( variable ),
                    "--" => PreDecrementAssign( variable ),
                    _ => throw new InvalidOperationException( $"Unsupported prefix operator: {op}." )
                };
            } );

        var postfixExpression = varIdentifier
            .And(
                OneOf(
                    Terms.Text( "++" ),
                    Terms.Text( "--" )
                )
            )
            .Then<Expression>( static parts =>
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
            accessorExpression
        ).Unary(
            (Terms.Char( '!' ), Not),
            (Terms.Char( '-' ), Negate)
        ).Named( "unary" );

        // Binary Expressions

        return expression.Parser = unaryExpression.LeftAssociative(
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

    }

    // Helper Parsers

    private static Parser<IReadOnlyList<Expression>> Arguments( Parser<Expression> expression )
    {
        return ZeroOrOne( Separated( Terms.Char( ',' ), expression ) )
            .Then( static arguments => arguments ?? Array.Empty<Expression>() );
    }

    // Variable Parsers

    private static Parser<Expression> AssignmentParser( Parser<Expression> expression )
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
            .Then<Expression>( static ( ctx, parts ) =>
                {
                    var (scope, _) = ctx;
                    var (ident, op, right) = parts;

                    var left = scope.LookupVariable( ident );

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

    private static Parser<Expression> DeclarationParser( Parser<Expression> expression )
    {
        return Terms.Text( "var" )
            .SkipAnd( Terms.Identifier() )
            .AndSkip( Terms.Char( '=' ) )
            .And( expression )
            .Then<Expression>( static ( ctx, parts ) =>
                {
                    var (scope, _) = ctx;
                    var (ident, right) = parts;

                    var left = ident.ToString()!;

                    var variable = Variable( right.Type, left );
                    scope.Variables.Add( left, variable );

                    return Assign( variable, right );
                }
            );
    }

    // Member Parsers

    private static Parser<Expression> IndexerAccessParser( Parser<Expression> baseExpression, Parser<Expression> expression )
    {
        return baseExpression
        .And(
            Between(
                Terms.Char( '[' ),
                Separated(
                    Terms.Char( ',' ),
                    expression
                ),
                Terms.Char( ']' )
            ) )
        .Then<Expression>( static parts =>
        {
            var (target, indexes) = parts;

            var indexer = target.Type
                .GetProperties()
                .FirstOrDefault( p => p.GetIndexParameters()
                    .Select( x => x.ParameterType )
                    .SequenceEqual( indexes.Select( i => i.Type ) ) );

            if ( indexer == null )
                throw new InvalidOperationException( $"No indexer found on type '{target.Type}' with {indexes.Count} parameters." );

            return Property( target, indexer, [.. indexes] );
        } );

    }

    private static Parser<Expression> MemberAccessParser( Parser<Expression> baseExpression, Parser<Expression> expression )
    {
        return baseExpression
            .AndSkip( Terms.Char( '.' ) )
            .And(
                Separated(
                    Terms.Char( '.' ),
                    Terms.Identifier().And(
                        ZeroOrOne(
                            Between(
                                Terms.Char( '(' ),
                                Arguments( expression ),
                                Terms.Char( ')' )
                            )
                        )
                    )
                )
            )
            .Then( static parts =>
            {
                const BindingFlags BindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

                var (current, accesses) = parts;

                foreach ( var (memberName, arguments) in accesses )
                {
                    var name = memberName.ToString()!;

                    var type = current switch
                    {
                        ConstantExpression ce => ce.Value as Type ?? ce.Type,
                        Expression e => e.Type,
                        _ => throw new InvalidOperationException( "Invalid target expression." )
                    };

                    if ( arguments != null )
                    {
                        // Resolve method call
                        var methodInfo = TypeResolver.FindMethod( type, name, arguments );

                        current = methodInfo?.IsStatic switch
                        {
                            true => Call( methodInfo, arguments.ToArray() ),
                            false => Call( current, methodInfo, arguments.ToArray() ),
                            null => throw new InvalidOperationException( $"Method '{name}' not found on type '{type}'." )
                        };
                    }
                    else
                    {
                        // Resolve property/field
                        var member = current.Type.GetMember( name, BindingAttr ).FirstOrDefault();

                        current = member?.MemberType switch
                        {
                            MemberTypes.Property => Property( current, (PropertyInfo) member ),
                            MemberTypes.Field => Field( current, (FieldInfo) member ),
                            null => throw new InvalidOperationException( $"Member '{name}' not found on type '{current.Type}'." ),
                            _ => throw new InvalidOperationException( $"Unsupported member type: {member.MemberType}." )
                        };
                    }
                }

                return current;
            } );
    }

    // Statement Parsers

    private static Parser<Expression> BreakParser()
    {
        return Terms.Text( "break" )
            .Then<Expression>( static ( ctx, _ ) =>
            {
                var (scope, _) = ctx;
                var breakLabel = scope.Frame.BreakLabel;

                if ( breakLabel == null )
                    throw new Exception( "Invalid use of 'break' outside of a loop or switch." );

                return Break( breakLabel );
            } );
    }

    private static Parser<Expression> ContinueParser()
    {
        return Terms.Text( "continue" )
            .Then<Expression>( static ( ctx, _ ) =>
            {
                var (scope, _) = ctx;
                var continueLabel = scope.Frame.ContinueLabel;

                if ( continueLabel == null )
                    throw new Exception( "Invalid use of 'continue' outside of a loop." );

                return Continue( continueLabel );
            } );
    }

    private static Parser<Expression> GotoParser()
    {
        return Terms.Text( "goto" )
            .SkipAnd( Terms.Identifier() )
            .Then<Expression>( static ( ctx, labelName ) =>
            {
                var (scope, _) = ctx;
                var label = scope.Frame.GetOrCreateLabel( labelName.ToString() );
                return Goto( label );
            } );
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
            } );
    }

    private static Parser<Expression> ReturnParser( Parser<Expression> expression )
    {
        return Terms.Text( "return" )
            .SkipAnd( ZeroOrOne( expression ) )
            .Then<Expression>( static ( ctx, returnValue ) =>
            {
                var (scope, _) = ctx;

                var returnType = returnValue?.Type ?? typeof( void );
                var returnLabel = scope.Frame.GetOrCreateReturnLabel( returnType );

                return returnType == typeof( void )
                    ? Return( returnLabel )
                    : Return( returnLabel, returnValue, returnType );
            } );
    }

    private static Parser<Expression> ThrowParser( Parser<Expression> expression )
    {
        return Terms.Text( "throw" )
            .SkipAnd( ZeroOrOne( expression ) )
            .Then<Expression>( static exceptionExpression =>
            {
                if ( exceptionExpression != null && !typeof( Exception ).IsAssignableFrom( exceptionExpression.Type ) )
                {
                    throw new InvalidOperationException(
                        $"Invalid throw argument: Expected an exception type, but found {exceptionExpression.Type}." );
                }

                return Throw( exceptionExpression );
            } );
    }


    private static Parser<Expression> ConditionalParser( Parser<Expression> expression, Deferred<Expression> statement )
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

    private static Parser<Expression> LoopParser( Deferred<Expression> statement )
    {
        var parser = Terms.Text( "loop" )
            .Then( ( ctx, _ ) =>
            {
                var (scope, _) = ctx;

                var breakLabel = Label( typeof( void ), "Break" );
                var continueLabel = Label( typeof( void ), "Continue" );

                scope.Push( FrameType.Child, breakLabel, continueLabel );

                return (breakLabel, continueLabel);
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
                var (scope, _) = ctx;

                var (breakLabel, continueLabel) = parts.Item1;
                var exprs = parts.Item2;

                try
                {
                    var body = Block( exprs );
                    return Loop( body, breakLabel, continueLabel );
                }
                finally
                {
                    scope.Pop();
                }
            } );

        return parser;
    }

    private static Parser<Expression> SwitchParser( Parser<Expression> expression, Deferred<Expression> statement )
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
            .Then( static ( ctx, _ ) =>
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
                    ZeroOrMany( caseParser ).And( ZeroOrOne( defaultParser ) ),
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
            } );

        return parser;
    }

    private static Parser<Expression> LambdaParser( Parser<Expression> identifier, Parser<Expression> expression, Deferred<Expression> statement )
    {
        var parameters = ZeroOrOne(
                Separated(
                    Terms.Char( ',' ),
                    identifier.And( Terms.Identifier() )
                )
            )
            .Then( static ( ctx, parts ) =>
            {
                var (scope, resolver) = ctx;

                if ( parts == null )
                    return [];

                scope.Push( FrameType.Method );

                return parts.Select( p =>
                {
                    var (typeName, paramName) = p;

                    var type = resolver.ResolveType( typeName.ToString() )
                        ?? throw new InvalidOperationException( $"Unknown type: {typeName}." );

                    var name = paramName.ToString()!;
                    var parameter = Parameter( type, name );

                    scope.Variables.Add( name, parameter );

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
                    expression,
                    Between(
                        Terms.Char( '{' ),
                        ZeroOrMany( statement ),
                        Terms.Char( '}' )
                    )
                    .Then<Expression>( static ( ctx, body ) =>
                    {
                        var (scope, _) = ctx;
                        var returnLabel = scope.Frame.ReturnLabel;

                        return Block( 
                            body.Concat( 
                                [Label( returnLabel, Default( returnLabel.Type ) )] 
                            ) 
                        );
                    } )
                )
            )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                var (args, body) = parts;

                try
                {
                    return Lambda( body, args );
                }
                finally
                {
                    if ( args.Length != 0 )
                        scope.Pop();
                }
            } );

        return parser;
    }

    private static Parser<Expression> TryCatchParser( Deferred<Expression> statement )
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
                            .Then( static ( ctx, parts ) =>
                            {
                                var (_, resolver) = ctx;
                                var (typeName, variableName) = parts;

                                var type = resolver.ResolveType( typeName.ToString()! );

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

    private static Parser<Expression> NewParser( Parser<Expression> expression )
    {
        var typeNameParser = Separated( Terms.Char( '.' ), Terms.Identifier() )
            .Then( static ( ctx, parts ) =>
            {
                var (_, resolver) = ctx;

                var typeName = string.Join( ".", parts );
                var type = resolver.ResolveType( typeName );

                if ( type == null )
                    throw new InvalidOperationException( $"Unknown type: {typeName}." );

                return type;
            } );

        // TODO: Add optional array initializer
        var parser = Terms.Text( "new" )
            .SkipAnd( typeNameParser )
            .And(
                OneOf(
                    Between(
                        Terms.Char( '(' ),
                        Arguments( expression ),
                        Terms.Char( ')' )
                    ).Then( static parts => (ConstructorType.Object, parts) ),
                    Between(
                        Terms.Char( '[' ),
                        Separated(
                            Terms.Char( ',' ),
                            expression
                        ),
                        Terms.Char( ']' )
                    )
                    //.And( arrayInitializer ) // TODO: Toggle between bounds and init if exists
                    .Then( static parts => (ConstructorType.ArrayBounds, parts) )
                )
            )
            .Then<Expression>( static parts =>
            {
                var (type, (constructorType, arguments)) = parts;  // TODO: Add initializer

                switch ( constructorType )
                {
                    case ConstructorType.ArrayBounds:
                        if ( arguments.Count == 0 )
                            throw new InvalidOperationException( "Array bounds initializer requires at least one argument." );

                        return NewArrayBounds( type, arguments );

                    case ConstructorType.ArrayInit:
                        throw new NotImplementedException( "Array initializer not implemented." );

                    //return NewArrayInit( type, arguments );

                    case ConstructorType.Object:
                        var constructor = type.GetConstructor( arguments.Select( arg => arg.Type ).ToArray() );

                        if ( constructor == null )
                            throw new InvalidOperationException( $"No matching constructor found for type {type.Name}." );

                        return New( constructor, arguments );

                    default:
                        throw new InvalidOperationException( $"Unsupported constructor type: {constructorType}." );
                }
            } );

        return parser;
    }

    private static Parser<Expression> LambdaInvokeParser( Parser<Expression> baseExpression )
    {
        var parser = Terms.Identifier()
            .And(
                Between(
                    Terms.Char( '(' ),
                    Arguments( baseExpression ),
                    Terms.Char( ')' )
                )
            )
            .Then<Expression>( static ( ctx, parts ) =>
            {
                var (scope, _) = ctx;
                var (targetName, invocationArguments) = parts;

                var targetExpression = scope.LookupVariable( targetName );

                return Invoke(
                    targetExpression,
                    invocationArguments
                );
            } );

        return parser;
    }

    private enum ConstructorType
    {
        Object,
        ArrayBounds,
        ArrayInit,
    }
}

