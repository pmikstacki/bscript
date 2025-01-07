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
    public XsParser Xs { get; set; } = new
    (
        new XsConfig
        {
            References = [Assembly.GetExecutingAssembly()],
            Extensions = [new ForParseExtension()]
        }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            for ( var i = 0; i < 10; i++ )
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
    public ExtensionType Type => ExtensionType.Complex;

    public KeyParserPair<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, assignable, statement) = binder;

        return new( "for",
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
                .Then<Expression>( static parts =>
                {
                    var ((initializer, test, iteration), body) = parts;

                    var bodyBlock = Block( body );
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

