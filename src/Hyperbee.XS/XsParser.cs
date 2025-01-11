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
// Add Async Await

public partial class XsParser
{
    private readonly Parser<Expression> _xs;
    private readonly XsConfig _config;

    public XsParser()
        : this( default )
    {
    }

    public XsParser( XsConfig config )
    {
        _config = config ?? new XsConfig();
        _xs = CreateParser( _config );
    }

    // Parse

    public Expression Parse( string script )
    {
        var scanner = new Scanner( script );
        var context = new XsContext( _config, scanner ) { WhiteSpaceParser = XsParsers.WhitespaceOrNewLineOrComment() };

        return _xs.Parse( context );
    }

    // Parsers

    private static Parser<Expression> CreateParser( XsConfig config )
    {
        var statement = Deferred<Expression>();

        // Expressions

        var expression = ExpressionParser( statement, config );

        var declaration = DeclarationParser( expression );
        var assignment = AssignmentParser( expression );

        var assignableExpression = OneOf(
            declaration,
            assignment,
            expression
        );

        // Compose Statements

        var statements = XsParsers.IdentifierLookup<Expression>();

        var expressionStatement = assignableExpression.AndSkip( Terms.Char( ';' ) );
        var label = LabelParser();

        statements.Add(
            BreakParser(),
            ContinueParser(),
            GotoParser(),
            ReturnParser( expression ),
            ThrowParser( expression ),
            ConditionalParser( expression, statement ),
            LoopParser( statement ),
            TryCatchParser( statement ),
            SwitchParser( expression, statement )
        );

        statements.Add(
            StatementExtensions( config, ExtensionType.Complex | ExtensionType.Terminated, expression, assignableExpression, statement )
        );

        statement.Parser = OneOf(
            label,
            statements,
            expressionStatement
        );

        // Create the final parser

        return XsParsers.Bounded(
            static ctx =>
            {
                var (scope, _) = ctx;
                scope.Push( FrameType.Parent );
            },
            ZeroOrMany( statement ).Then<Expression>( static ( ctx, statements ) =>
            {
                var (scope, _) = ctx;
                return ConvertToFinalExpression( statements, scope );
            } ),
            static ctx =>
            {
                var (scope, _) = ctx;
                scope.Pop();

                // Ensure we've reached the end of the script
                var cursor = ctx.Scanner.Cursor;
                ctx.SkipWhiteSpace();

                if ( cursor.Eof == false )
                    throw new SyntaxException( "Syntax Error. Failure parsing script.", cursor );
            }
        );

        static KeyParserPair<Expression>[] StatementExtensions(
                XsConfig config,
                ExtensionType type,
                Parser<Expression> expression,
                Parser<Expression> assignableExpression,
                Deferred<Expression> statement )
        {
            var binder = new ExtensionBinder( config, expression, assignableExpression, statement );

            return binder.Config.Extensions
                .Where( x => type.HasFlag( x.Type ) )
                .OrderBy( x => x.Type )
                .Select( x => new KeyParserPair<Expression>( x.Key, x.CreateParser( binder ) ) )
                .ToArray();
        }
    }

    private static Parser<Expression> ExpressionParser( Deferred<Expression> statement, XsConfig config )
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

        var charaterLiteral = Terms.String( StringLiteralQuotes.Single )
            .Then<Expression>( static value =>
            {
                if ( value.Length != 1 )
                    throw new InvalidOperationException( "Character literal must be a single character." );

                return Constant( value.Span[0] );
            } );

        var stringLiteral = Terms.String( StringLiteralQuotes.Double )
            .Then<Expression>( static value => Constant( value.ToString() ) );

        var nullLiteral = Terms.Text( "null" ).Then<Expression>( static _ => Constant( null ) );

        var literal = OneOf(
            longLiteral,
            doubleLiteral,
            floatLiteral,
            integerLiteral,
            charaterLiteral,
            stringLiteral,
            booleanLiteral,
            nullLiteral
        ).Or(
            OneOf(
                LiteralExtensions( config, expression, statement )
            )
        ).Named( "literal" );

        // Identifiers

        var variable = XsParsers.Variable();
        var typeConstant = XsParsers.TypeConstant();

        var identifier = OneOf(
            variable,
            typeConstant
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

        //primaryExpression.Parser = OneOf(
        //    primaryExpression.AndMaybe( expr =>
        //        OneOf(
        //            MemberAccessParser( expr, expression ),
        //            LambdaInvokeParser( expr, expression ),
        //            IndexerAccessParser( expr, expression )
        //        )
        //    ),
        //    baseExpression
        //).Named( "primary" );

        primaryExpression.Parser = baseExpression.LeftAssociative(
            left => MemberAccessParser( left, expression ),
            left => LambdaInvokeParser( left, expression ),
            left => IndexerAccessParser( left, expression )
        ).Named( "primary" );

        // Prefix and Postfix Expressions

        var prefixExpression = OneOf(
                Terms.Text( "++" ),
                Terms.Text( "--" )
            )
            .And( variable )
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

        var postfixExpression = variable
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

        // Cast Expressions

        var castExpression = baseExpression
            .And(
                OneOf(
                    Terms.Text( "as?" ),
                    Terms.Text( "as" ),
                    Terms.Text( "is" )
                )
                .And( typeConstant )
            )
            .Then<Expression>( static parts =>
            {
                var (expr, (op, expression)) = parts;

                if ( expression is not ConstantExpression constantExpression || constantExpression.Value is not Type type )
                    throw new InvalidOperationException( $"The '{op}' operator must be followed by a valid type." );

                return op switch
                {
                    "as?" => TypeAs( expr, typeof( Nullable<> ).MakeGenericType( type ) ),
                    "as" => Convert( expr, type ),
                    "is" => TypeIs( expr, type ),
                    _ => throw new NotImplementedException(),
                };
            } );

        // Unary Expressions

        var unaryExpression = OneOf(
            prefixExpression,
            postfixExpression,
            castExpression,
            primaryExpression
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

        static Parser<Expression>[] LiteralExtensions(
            XsConfig config,
            Parser<Expression> expression,
            Deferred<Expression> statement )
        {
            var binder = new ExtensionBinder( config, expression, null, statement );

            return binder.Config.Extensions
                .Where( x => ExtensionType.Literal.HasFlag( x.Type ) )
                .OrderBy( x => x.Type )
                .Select( x => x.CreateParser( binder ) )
                .ToArray();
        }
    }

    // Helper Parsers

    private static Parser<IReadOnlyList<Expression>> ArgumentsParser( Parser<Expression> expression )
    {
        return ZeroOrOne( Separated( Terms.Char( ',' ), expression ) )
            .Then( static arguments => arguments ?? Array.Empty<Expression>() );
    }

    private static Parser<IReadOnlyList<Type>> TypeArgsParser()
    {
        return ZeroOrOne( Separated( Terms.Char( ',' ), XsParsers.TypeRuntime() ) )
            .Then( static typeArgs => typeArgs ?? Array.Empty<Type>() );
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

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Type ConvertToType( Expression expression )
    {
        ArgumentNullException.ThrowIfNull( expression, nameof( expression ) );

        return expression switch
        {
            ConstantExpression ce => ce.Value as Type ?? ce.Type,
            Expression => expression.Type
        };
    }
}

