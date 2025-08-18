﻿using System.Linq.Expressions;
using Hyperbee.XS.Core.Writer;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserComplexTests
{
    public static XsParser Xs { get; set; } = new( TestInitializer.XsConfig );

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldDemonstrateAllLanguageFeatures( CompilerType compiler )
    {
        const string xs =
        """
        var results = new List<int>(5);

        var c = 0; var c1 = 0; var c2 = 0;
        if (1 + 1 == 2)
        {
            c = if (true) { 42; } else { 0; };
        }
        else
        {
            c = 1;
        }
        results.Add(c);
        
        var s = 3;
        switch (s)
        {
            case 1: s = 1; goto there;
            case 2: s = 2; goto there;
            default: s = 42; goto there;
        }
        there:
        results.Add(s);
        
        var t = 1;
        try
        {
            throw new ArgumentException();
        }
        catch (InvalidOperationException)
        {
            t = 0;
        }
        catch (ArgumentException)
        {
            t += 40;
        }
        finally
        {
            t += 1;
        }
        results.Add(t);

        var l = 0;
        loop
        {
            l++; 
            if( l == 42 )
            {
                break;
            }
        }
        results.Add(l);

        var calc = (int a, int b) => a * b;
        results.Add( calc(6, 7) );

        results;
        """;

        var debugger = new XsDebugger()
        {
            Handler = d =>
            {
                Console.WriteLine( $"Line: {d.Line}, Column: {d.Column}, Variables: {d.Variables}, Text: {d.SourceLine}" );
            },
            Breakpoints = [
                new( 1 ),                // all of line 1   
                new( 3, new( 12, 23 ) ), // line 3, between columns 12-23
            ]
        };

        var expression = Xs.Parse( xs, debugger );

        var expressionString = expression.ToExpressionString();

        Console.WriteLine( "XS:" );
        Console.WriteLine( xs );

        Console.WriteLine( "\nExpression:" );
        Console.WriteLine( expressionString );

        var lambda = Expression.Lambda<Func<List<int>>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        // Assertions for each feature
        Assert.AreEqual( 5, result.Count ); // total number of features

        Assert.AreEqual( 42, result[0] ); // If-Else logic
        Assert.AreEqual( 42, result[1] ); // Switch-Case logic
        Assert.AreEqual( 42, result[2] ); // Try-Catch-Finally
        Assert.AreEqual( 42, result[3] ); // Loop 
        Assert.AreEqual( 42, result[4] ); // Lambda calculation (6 * 7)
    }
}
