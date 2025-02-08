using System.Linq.Expressions;
using Hyperbee.Xs.Extensions;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class PackageParseExtensionTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        const string script =
            """
            package Humanizer.Core;
            using Humanizer;
            
            var number = 123;
            number.ToWords();
            """;

        var xsConfig = new XsConfig()
        {
            Extensions = [new PackageParseExtension()]
        };

        var xs = new XsParser( xsConfig );
        var expression = xs.Parse( script );

        var lambda = Expression.Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "one hundred and twenty-three", result );
    }
}
