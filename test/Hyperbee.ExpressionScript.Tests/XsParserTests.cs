using Hyperbee.XS.Parsers;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Tests;

[TestClass]
public class XsParserTests
{
    [TestMethod]
    public void Compile_ShouldSucceed_Constant()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "5;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNegate()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "-1 + 3;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 2, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "!false;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_UnaryNot_Grouping()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "!(false);" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "10 + 12;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 22, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMultiple()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "10 + 12 + 14;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 36, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithGrouping()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "(10 + 12) * 2;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryAdd_WithMulitpleGrouping()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "(10 + 12) * (1 + 1);" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 44, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_BinaryLessThan()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "10 < 11;" );

        var lambda = Lambda<Func<bool>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.IsTrue( result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariable()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10;" );

        var lambda = Lambda( expression );

        var compiled = lambda.Compile();

        Assert.IsNotNull( compiled );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x + 10;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 20, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndAssignmentResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x = x + 10; x + 22;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAddAssignmentResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x += 32;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPostResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; x++;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result ); // x++ returns the value before increment
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithVariableAndPrefixResult()
    {
        var parser = new XsParser();
        var expression = parser.Parse( "var x = 10; ++x;" );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 11, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditional()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
        """
        if (true)
        {
            "hello";
        } 
        else
        { 
            "goodBye";
        }
        """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalAndNoElse()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = "goodbye";
            if (true)
            {
                x = "hello";
            }
            x; 
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithConditionalVariable()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 10;
            if ( x == (9 + 1) )
            {
                "hello";
            } 
            else
            { 
                "goodBye";
            }
            """ );

        var lambda = Lambda<Func<string>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( "hello", result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithLoop()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 0;
            loop
            {
                x++; // do something
                if( x == 10 )
                {
                    break;
                }
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 10, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithTryCatch()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 0;
            try
            {
                x = 32; // do something
            }
            catch( Exception e )
            {
                x -= 10;
            }
            finally
            {
                x+= 10;
            }
            x;
            """ );

        var lambda = Lambda<Func<int>>( expression );

        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchCaseOnly()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 3;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 1;
                    break;
                case 3:
                    result = 42;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 42, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchDefaultOnly()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 3;
            var result = 0;

            switch (x)
            {
                default:
                    result = 42;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>(expression);
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchCasesAndDefault()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 10;
                    break;
                case 2:
                    result = 20;
                    break;
                default:
                    result = 30;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>( expression );
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual( 20, result );
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchFallthroughToDefault()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 99;
            var result = 0;

            switch (x)
            {
                case 1:
                    result = 10;
                    break;
                case 2:
                    result = 20;
                    break;
                default:
                    result = 30;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>(expression);
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchMultipleCaseLabels()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
            """
            var x = 2;
            var result = 0;

            switch (x)
            {
                case 1:
                case 2:
                    result = 100;
                    break;
                default:
                    result = 0;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>(expression);
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual(100, result);
    }

    [TestMethod]
    public void Compile_ShouldSucceed_WithSwitchNestedSwitchStatements()
    {
        var parser = new XsParser();
        var expression = parser.Parse(
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
                            break;
                        default:
                            result = 0;
                            break;
                    }
                    break;
                default:
                    result = 0;
                    break;
            }

            result;
            """ );

        var lambda = Lambda<Func<int>>(expression);
        var compiled = lambda.Compile();
        var result = compiled();

        Assert.AreEqual(50, result);
    }
}

