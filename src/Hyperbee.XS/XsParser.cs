using System.Linq.Expressions;
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
// Add Array access
// Add Generics
// Add Cast As Is Operators
// Add Async Await

public partial class XsParser
{
    private static readonly Parser<Expression> __xs = CreateParser();

    // Parse

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

    private static BlockExpression ConvertToFinalExpression( IReadOnlyList<Expression> expressions, ParseScope scope )
    {
        var returnLabel = scope.Frame.ReturnLabel;
        var finalType = expressions.Count > 0 ? expressions[^1].Type : null;

        if ( returnLabel == null )
        {
            return Block( scope.Variables.EnumerateValues(), expressions );
        }

        if ( returnLabel.Type != finalType )
        {
            throw new InvalidOperationException( $"Mismatched return types: Expected {returnLabel.Type}, found {finalType}." );
        }

        return Block( scope.Variables.EnumerateValues(), expressions.Concat( [Label( returnLabel, Default( returnLabel.Type ) )] ) );
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
            ZeroOrMany( statement ).Then<Expression>( static ( ctx, statements ) =>
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
                    throw new SyntaxException( "Syntax Error. Failure parsing script.", cursor );

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
        ).Named( "base" );

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
        ).Named( "indexer" );

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
        ).Named( "expression" );

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
        return XsParsers.IfIdentifier( "var",
            Terms.Identifier()
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
            )
        );
    }

    private static Parser<Expression> NewParser( Parser<Expression> expression )
    {
        // TODO: Add optional array initializer

        return XsParsers.IfIdentifier( "new",
            TypeNameParser()
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
            } ) );

        static Parser<Type> TypeNameParser()
        {
            return Separated( Terms.Char( '.' ), Terms.Identifier() )
                .Then( static ( ctx, parts ) =>
                {
                    var (_, resolver) = ctx;

                    var typeName = string.Join( ".", parts );
                    var type = resolver.ResolveType( typeName );

                    if ( type == null )
                        throw new InvalidOperationException( $"Unknown type: {typeName}." );

                    return type;
                } );
        }
    }

    private enum ConstructorType
    {
        Object,
        ArrayBounds,
        ArrayInit,
    }
}

