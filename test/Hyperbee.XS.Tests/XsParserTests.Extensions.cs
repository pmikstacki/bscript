using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.XS.System;
using Hyperbee.XS.System.Parsers;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserExtensionsTests
{
    public XsParser Xs { get; set; } = new
    (
        new XsConfig
        {
            References = [Assembly.GetExecutingAssembly()],
            Extensions = [new AnswerToEverythingParseExtension()]
        }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = Xs.Parse( "answer; // answer to everything" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }
}

public class AnswerToEverythingParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Complex;

    public KeyParserPair<Expression> CreateParser( ExtensionBinder binder )
    {
        var (_, expression, assignable, statement) = binder;

        return new( "answer",
            Always()
            .AndSkip( Terms.Char( ';' ) )
            .Then<Expression>( static ( _, _ ) => Constant( 42 ) )
        );
    }
}

