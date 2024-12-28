using System.Linq.Expressions;
using Parlot.Fluent;

namespace Hyperbee.XS;

public interface IParserExtension
{
    void Extend( Parser<Expression> parser );
}
