namespace Hyperbee.XS.Tests;

public class SimpleClass
{
    public int Value { get; }
    
    public int ReturnValue() => Value;
    public int AddNumbers( int x, int y ) => x + y;

    public static int StaticAddNumbers( int x, int y ) => x + y;

    public SimpleClass( int value )
    {
        Value = value;
    }
}
