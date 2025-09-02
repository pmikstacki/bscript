
using static System.Linq.Expressions.Expression;

namespace bscript.Tests;

[TestClass]
public class XsParserNewTests
{
    public static BScriptParser BScript { get; set; } = new( TestInitializer.BScriptConfig );

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNewExpression( CompilerType compiler )
    {
        var expression = BScript.Parse(
            """
            using bscript.Tests;
            new TestClass(42);
            """ );

        var lambda = Lambda<Func<TestClass>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsNotNull( result );
        Assert.AreEqual( 42, result.PropertyValue );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNewAndProperty( CompilerType compiler )
    {
        try
        {
            var expression = BScript.Parse(
                """
                using bscript.Tests;
                new TestClass(42).PropertyThis.PropertyValue;
                """ );

            var lambda = Lambda<Func<int>>( expression );

            var function = lambda.Compile( compiler );
            var result = function();

            Assert.AreEqual( 42, result );
        }
        catch ( SyntaxException se )
        {
            Assert.Fail( se.Message );
        }
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNewArray( CompilerType compiler )
    {
        var expression = BScript.Parse(
            """
            new int[5];
            """ );

        var lambda = Lambda<Func<int[]>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 5, result.Length );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNewMultiDimensionalArray( CompilerType compiler )
    {
        var expression = BScript.Parse(
            """
            new int[2,5];
            """ );

        var lambda = Lambda<Func<int[,]>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 10, result.Length );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNewArrayInit( CompilerType compiler )
    {
        var parser = new BScriptParser();

        var expression = parser.Parse(
            """
            new int[] {1,2};
            """ );

        var lambda = Lambda<Func<int[]>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 2, result.Length );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNewListInit( CompilerType compiler )
    {
        var parser = new BScriptParser();

        var expression = parser.Parse(
            """
            new List<int>() {1,2};
            """ );

        var lambda = Lambda<Func<List<int>>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 2, result.Count );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithNewJaggedArray( CompilerType compiler )
    {
        var parser = new BScriptParser();

        var expression = parser.Parse(
            """
            new int[] { 
                new int[] {10,20,30}, 
                new int[] {40,50}, 
                new int[] {60} 
            };
            """ );

        var lambda = Lambda<Func<int[][]>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 3, result.Length );
        Assert.AreEqual( 3, result[0].Length );
        Assert.AreEqual( 10, result[0][0] );
        Assert.AreEqual( 2, result[1].Length );
        Assert.AreEqual( 40, result[1][0] );
        Assert.AreEqual( 1, result[2].Length );
        Assert.AreEqual( 60, result[2][0] );

    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithGeneric( CompilerType compiler )
    {
        var expression = BScript.Parse(
            """
            new List<int>();
            """ );

        var lambda = Lambda<Func<List<int>>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsInstanceOfType<List<int>>( result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithDefaultValue( CompilerType compiler )
    {
        var expression = BScript.Parse(
            """
            var x = default( int );
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 0, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithDefaultReference( CompilerType compiler )
    {
        var expression = BScript.Parse(
            """
            var x = default( bscript.Tests.TestClass );
            x;
            """ );

        var lambda = Lambda<Func<TestClass>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.IsNull( result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithDefaultInvalid( CompilerType compiler )
    {
        try
        {
            BScript.Parse(
                """
                var x = 5;
                var y = default(wrong};
                x + y;
                """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidImport( CompilerType compiler )
    {
        try
        {
            BScript.Parse(
            """
            using ;
            new TestClass(42);
            """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithInvalidImportMissingIdentifier( CompilerType compiler )
    {
        try
        {
            BScript.Parse(
            """
            using bscript.XS.;
            """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }
}
