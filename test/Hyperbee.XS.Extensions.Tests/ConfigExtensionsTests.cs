using Hyperbee.Expressions;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Extensions.Tests;

[TestClass]
public class ConfigExtensionsTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [TestMethod]
    public void Compile_ShouldSucceed_WithConfigDefaultType()
    {
        var serviceProvider = ServiceProvider.GetServiceProvider();
        var expression = Xs.Parse(
            """
            config::hello;
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile( serviceProvider );
        var result = compiled();

        Assert.AreEqual( "Hello, World!", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConfigWithType()
    {
        var serviceProvider = ServiceProvider.GetServiceProvider();
        var expression = Xs.Parse(
            """
            config<int>::number;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile( serviceProvider );
        var result = compiled();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConfigNestedKeys()
    {
        var serviceProvider = ServiceProvider.GetServiceProvider();
        var expression = Xs.Parse(
            """
            config<bool>::connections.sql.secure;
            """ );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile( serviceProvider );
        var result = compiled();

        Assert.IsTrue( result );
    }
}
