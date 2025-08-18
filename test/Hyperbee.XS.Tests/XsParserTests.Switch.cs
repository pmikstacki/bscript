﻿using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserSwitchTests
{
    public static XsParser Xs { get; } = new();

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithSwitchCaseOnly( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 3;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 1;
                    goto there;
                case 3:
                    result = 42;
                    goto there;
            }
            there:
            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithSwitchDefaultOnly( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 3;
            var result = 0;

            switch (x)
            {
                default:
                    result = 42;
                    goto there;
            }
            there:
            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 42, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithSwitchCasesAndDefault( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 10;
                    goto there;
                case 2:
                    result = 20;
                    goto there;
                default:
                    result = 30;
                    goto there;
            }
            there:
            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 20, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithSwitchFallthroughToDefault( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 99;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 10;
                    goto there;
                case 2:
                    result = 20;
                    goto there;
                case 3:
                    result = 30;
                    goto there;
                default:
                    result = 50;
                    goto there;
            }
            there:
            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 50, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithSwitchMultipleCaseLabels( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                case 2:
                    result = 100;
                    goto there;
                default:
                    result = 0;
                    goto there;
            }
            there:
            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 100, result );
    }

    [DataTestMethod]
    [DataRow( CompilerType.Fast )]
    [DataRow( CompilerType.System )]
    [DataRow( CompilerType.Interpret )]
    public void Compile_ShouldSucceed_WithSwitchNestedSwitchStatements( CompilerType compiler )
    {
        var expression = Xs.Parse(
            """
            var x = 1;
            var y = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                    switch (y)
                    {
                        case 2:
                            result = 50;
                            goto here;
                        default:
                            result = 0;
                            goto here;
                    }
                    here:
                    goto there;
                default:
                    result = 0;
                    goto there;
            }
            there:
            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var function = lambda.Compile( compiler );
        var result = function();

        Assert.AreEqual( 50, result );
    }
}

