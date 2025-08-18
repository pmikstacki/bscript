﻿using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserLoopTests
{
    public static XsParser Xs { get; } = new();

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithLoop( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 0;
            loop
            {
                x++; 
                if( x == 10 )
                {
                    break;
                }
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    [ExpectedException( typeof( SyntaxException ) )]
    public void Compile_ShouldFail_WithMissingBrace()
    {
        try
        {
            Xs.Parse(
            """
            var x = 0;
            loop
            
                x++; 
                if( x == 10 )
                {
                    break;
                }

            x;
            """ );
        }
        catch ( SyntaxException ex )
        {
            Console.WriteLine( ex.Message );
            throw;
        }
    }
}

