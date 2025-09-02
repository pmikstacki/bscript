#nullable enable
using System.Linq.Expressions;
using bscript.Core;
using bscript.Core.Ast;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace bscript;

public partial class BScriptParser
{
    // Parses a single AST expression. Currently supports only literals; will expand incrementally.
    public Expr ParseAstExpression(string script, BScriptDebugger debugger = null, ParseScope scope = null)
    {
        var scanner = new Scanner(script);
        var context = new BScriptContext(_config, debugger, scanner, scope) { WhiteSpaceParser = WhitespaceOrNewLineOrComment() };

        try
        {
            var expr = Deferred<Expr>();
            // For now, the AST pipeline only supports literals
            expr.Parser = LiteralAstParser(_config, expr);
            return expr.Parse(context);
        }
        catch (ParseException ex)
        {
            throw new SyntaxException(ex.Message, context.Scanner.Cursor);
        }
    }

    // Convenience bridge: parse to AST then emit to Expression. Falls back to legacy parser if AST fails.
    public Expression ParseToExpression(string script, BScriptDebugger debugger = null, ParseScope scope = null)
    {
        try
        {
            var ast = ParseAstExpression(script, debugger, scope);
            var emitter = new ExpressionEmitter(_config.Resolver);
            return emitter.Emit(ast);
        }
        catch (SyntaxException)
        {
            // Fallback to legacy expression-based parser to keep backward compatibility during migration
            return Parse(script, debugger, scope);
        }
    }
}
