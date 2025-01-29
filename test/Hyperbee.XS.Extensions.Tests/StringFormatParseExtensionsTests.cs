using System.Reflection;
using Hyperbee.Xs.Extensions;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class StringFormatParseExtensionTests
{
    public XsParser XsParser { get; set; } = new
    (
        new XsConfig
        {
            References = [Assembly.GetExecutingAssembly()],
            Extensions = XsExtensions.Extensions()
        }
    );

    [TestMethod]
    public void Compile_ShouldSucceed_WithExtensions()
    {
        var expression = XsParser.Parse(
            """
            var x = "hello";
            var y = "!";
            var result = `{x} world{y}`;
            result;
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello world!", result );
    }
}


