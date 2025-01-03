namespace Hyperbee.XS.Tests;

public class TestClass
{
    public int PropertyValue { get; set; }
    public int MethodValue() => PropertyValue;

    public TestClass PropertyThis => this;
    public TestClass MethodThis() => this;

    public int AddNumbers( int x, int y ) => x + y;

    public static int StaticAddNumbers( int x, int y ) => x + y;

    public TestClass( int value )
    {
        PropertyValue = value;
    }
}
