using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.XS.System.Parsers;

internal static partial class XsParsers
{
    public static Parser<IReadOnlyList<T>> ListOfOne<T>( Parser<T> parser )
    {
        return OneOf( parser ).Then<IReadOnlyList<T>>( x => new List<T> { x } );
    }
}
