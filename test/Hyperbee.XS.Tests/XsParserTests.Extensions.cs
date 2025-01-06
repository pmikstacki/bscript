using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Expressions;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserExtensions
{
    [AssemblyInitialize]
    public static void AssemblyInitialize( TestContext testContext )
    {
        XsConfig.Extensions = new List<IParseExtension>() { new ForParseExtension() };
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var config = new XsConfig { References = [Assembly.GetExecutingAssembly()] };
        var parser = new XsParser();

        var expression = parser.Parse( config,
            """
            var x = 0;
            var i = 0;
            for ( i = 0; i < 10; i++ )
            {
                x++;
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result );
    }
}

public class ForParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.ComplexStatement;

    public Parser<Expression> Parser( ExtensionBinder binder )
    {
        var (expression, assignable, statement) = binder;

        return XsParsers.IfIdentifier( "for",
            XsParsers.Bounded(
                static ctx =>
                {
                    var (scope, _) = ctx;
                    scope.Push( FrameType.Method );
                },
                Between(
                    Terms.Char( '(' ),
                        assignable.AndSkip( Terms.Char( ';' ) )
                            .And( expression ).AndSkip( Terms.Char( ';' ) )
                            .And( expression ),
                    Terms.Char( ')' )
                )
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
                    var ((initializer, test, iteration), body) = parts;

                    var bodyBlock = Block( /*scope.Variables.EnumerateValues(),*/ body );
                    return ExpressionExtensions.For( initializer, test, iteration, bodyBlock );
                } ),
                static ctx =>
                {
                    var (scope, _) = ctx;
                    scope.Pop();
                }
            )
        );
    }
}

