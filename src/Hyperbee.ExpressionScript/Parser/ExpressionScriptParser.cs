using System.Linq.Expressions;
using Parlot.Fluent;

using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.ExpressionScript.Parser;

public class ExpressionScriptParser
{
    public static readonly Parser<BinaryExpression> Script;

    static ExpressionScriptParser()
    {
        var Identifier = Terms.Identifier();
        var IntegerLiteral = Terms.Integer();

        // Primary expressions
        var Primary = Identifier.Then( name => Parameter( typeof( long ), name.ToString() ) );

        // Variable declarations
        var VariableDeclaration = Terms.Text( "let" ).SkipAnd( Identifier )
            .AndSkip( Terms.Text( "=" ) )
            .And( IntegerLiteral ).Then( parts =>
            {

                var variable = Parameter( typeof( long ), parts.Item1.ToString() );
                return Assign( variable, Constant( parts.Item2 ) );
            } );

        Script = VariableDeclaration;
    }

    public static Expression Parse( string script )
    {
        if ( Script.TryParse( script, out var result ) )
        {
            return result;
        }
        return null;
    }

}
