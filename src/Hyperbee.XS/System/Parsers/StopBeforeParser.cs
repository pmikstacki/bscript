using Parlot;
using Parlot.Fluent;

namespace Hyperbee.XS.System.Parsers;

// StopBefore allows parsing a sequence of elements until a specific
// stopping condition is encountered.
// 
// It wraps an inner parser and a stopping condition parser. 
// If the stopping condition matches at the current position in the input, 
// parsing halts and the inner parser does not consume the stopping input.
// 
// This is particularly useful for scenarios like parsing statements 
// in a block or case clauses in a switch, where the end of the sequence 
// is indicated by specific keywords or symbols.
//
// Example Usage:
// 
// ZeroOrMany(
//   statement.AssertBefore( CaseUntil() )
// )
//
// In this example:
// - `statement` is parsed repeatedly.
// - Parsing stops when `CaseUntil()` matches (e.g., "case", "default", or "}").

public class StopBeforeParser<T, U> : Parser<T>
{
    private readonly Parser<T> _innerParser;
    private readonly Parser<U> _stoppingCondition;

    public StopBeforeParser( Parser<T> innerParser, Parser<U> stoppingCondition )
    {
        _innerParser = innerParser ?? throw new ArgumentNullException( nameof(innerParser) );
        _stoppingCondition = stoppingCondition ?? throw new ArgumentNullException( nameof(stoppingCondition) );
    }

    public override bool Parse( ParseContext context, ref ParseResult<T> result )
    {
        context.EnterParser( this );

        var current = context.Scanner.Cursor.Position;

        var stoppingCheck = new ParseResult<U>();
        if ( _stoppingCondition.Parse( context, ref stoppingCheck ) )
        {
            context.Scanner.Cursor.ResetPosition( current );
            context.ExitParser( this );
            return false;
        }

        var success = _innerParser.Parse( context, ref result );
        context.ExitParser( this );
        return success;
    }
}

public static class ParserExtensions
{
    public static Parser<T> StopBefore<T, U>( this Parser<T> parser, Parser<U> stoppingCondition )
    {
        return new StopBeforeParser<T, U>( parser, stoppingCondition );
    }
}
