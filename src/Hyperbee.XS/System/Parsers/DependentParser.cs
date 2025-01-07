using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

public sealed class DependentParser<T, U> : Parser<U>
{
    private readonly Parser<T> _parent;
    private readonly Func<T, ParseContext, Parser<U>> _childFactory;

    public DependentParser( Parser<T> parent, Func<T, Parser<U>> childFactory )
    {
        _parent = parent ?? throw new ArgumentNullException( nameof( parent ) );
        _childFactory = ( parentValue, _ ) => childFactory( parentValue ) ?? throw new ArgumentNullException( nameof( childFactory ) );
    }

    public DependentParser( Parser<T> parent, Func<T, ParseContext, Parser<U>> childFactory )
    {
        _parent = parent ?? throw new ArgumentNullException( nameof( parent ) );
        _childFactory = childFactory ?? throw new ArgumentNullException( nameof( childFactory ) );
    }

    public override bool Parse( ParseContext context, ref ParseResult<U> result )
    {
        context.EnterParser( this );

        var parentResult = new ParseResult<T>();

        if ( _parent.Parse( context, ref parentResult ) )
        {
            var child = _childFactory( parentResult.Value, context );
            var startPosition = context.Scanner.Cursor.Position;

            if ( child.Parse( context, ref result ) )
            {
                context.ExitParser( this );
                return true;
            }

            context.Scanner.Cursor.ResetPosition( startPosition );
        }

        context.ExitParser( this );
        return false;
    }
}

public static partial class XsParsers
{
    public static DependentParser<T, U> Dependent<T, U>( Parser<T> parent, Func<T, Parser<U>> childFactory )
    {
        return new DependentParser<T, U>( parent, childFactory );
    }

    public static DependentParser<T, U> Dependent<T, U>( Parser<T> parent, Func<T, ParseContext, Parser<U>> childFactory )
    {
        return new DependentParser<T, U>( parent, childFactory );
    }
}
