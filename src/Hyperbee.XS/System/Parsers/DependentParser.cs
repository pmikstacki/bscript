using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.System.Parsers;

public sealed class DependentParser<T1, T2> : Parser<ValueTuple<T1, T2>>
{
    private readonly Parser<T1> _parent;
    private readonly Parser<T2> _child;

    public DependentParser( Parser<T1> parent, Parser<T2> child )
    {
        _parent = parent ?? throw new ArgumentNullException( nameof(parent) );
        _child = child ?? throw new ArgumentNullException( nameof(child) );

        Name = $"Dependent({parent.Name}, {child.Name})";
    }

    public override bool Parse( ParseContext context, ref ParseResult<ValueTuple<T1, T2>> result )
    {
        try
        {
            context.EnterParser( this );

            var parentResult = new ParseResult<T1>();

            if ( !_parent.Parse( context, ref parentResult ) )
            {
                return false;
            }

            var childResult = new ParseResult<T2>();

            if ( _child.Parse( context, ref childResult ) )
            {
                result.Set( parentResult.Start, childResult.End, new ValueTuple<T1, T2>( parentResult.Value, childResult.Value ) );
            }
            else
            {
                result.Set( parentResult.Start, parentResult.End, new ValueTuple<T1, T2>( parentResult.Value, default ) );
            }

            return true;

        }
        finally
        {
            context.ExitParser( this );
        }
    }
}

public static partial class XsParsers
{
    public static Parser<ValueTuple<T, U>> AndThen<T, U>( this Parser<T> parser, Parser<U> next )
    {
        return parser.Then( ( ctx, parts ) => new ValueTuple<T, U>( parts, next.Parse( ctx ) ) );
    }

    public static Parser<T> AndAdvance<T>( this Parser<T> parser )
    {
        return parser.AndSkip( Always() );
    }
}
