#nullable enable
using bscript.Core.Ast;
using bscript.Core.Parsers;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace bscript;

public partial class BScriptParser
{
    // AST literal parser (not yet wired into the main parser pipeline)
    internal static Parser<Expr> LiteralAstParser(BScriptConfig config, Deferred<Expr> expression)
    {
        var integerLiteral = Terms.Number<int>(NumberOptions.AllowLeadingSign)
            .AndSkip(ZeroOrOne(Terms.Text("N", caseInsensitive: true)))
            .Then<Expr>(static value => new Literal(value, new TypeRef(typeof(int))));

        var longLiteral = Terms.Number<long>(NumberOptions.AllowLeadingSign)
            .AndSkip(Terms.Text("L", caseInsensitive: true))
            .Then<Expr>(static value => new Literal(value, new TypeRef(typeof(long))));

        var floatLiteral = Terms.Number<float>(NumberOptions.Float)
            .AndSkip(Terms.Text("F", caseInsensitive: true))
            .Then<Expr>(static value => new Literal(value, new TypeRef(typeof(float))));

        var doubleLiteral = Terms.Number<double>(NumberOptions.Float)
            .AndSkip(Terms.Text("D", caseInsensitive: true))
            .Then<Expr>(static value => new Literal(value, new TypeRef(typeof(double))));

        var booleanLiteral = Terms.Text("true").Or(Terms.Text("false"))
            .Then<Expr>(static value => new Literal(bool.Parse(value), new TypeRef(typeof(bool))));

        var characterLiteral = Terms.CharQuoted(StringLiteralQuotes.Single)
            .Then<Expr>(static value => new Literal(value, new TypeRef(typeof(char))));

        var stringLiteral = Terms.String(StringLiteralQuotes.Double)
            .Then<Expr>(static value => new Literal(value.ToString(), new TypeRef(typeof(string))));

        var rawStringLiteral = new RawStringParser()
            .Then<Expr>(static value => new Literal(value.ToString(), new TypeRef(typeof(string))));

        var nullLiteral = Terms.Text("null")
            .Then<Expr>(static _ => new Literal(null, new TypeRef(typeof(object))));

        var literal = OneOf(
            longLiteral,
            doubleLiteral,
            floatLiteral,
            integerLiteral,
            rawStringLiteral,
            characterLiteral,
            stringLiteral,
            booleanLiteral,
            nullLiteral
        ).Named("literal-ast");

        return literal;
    }
}
