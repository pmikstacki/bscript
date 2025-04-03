using System.Linq.Expressions;
using Hyperbee.Expressions;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Hyperbee.XS.Core.Writer;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Xs.Extensions;

public class ConfigurationParseExtension : IParseExtension, IExpressionWriter, IXsWriter
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "config";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        // var mongo = config::connections.mongo;
        // var isSecure = config<bool>::connections.sql.secure;

        return ZeroOrOne(
                Between(
                    Terms.Char( '<' ),
                    XsParsers.TypeRuntime(),
                    Terms.Char( '>' )
                )
            )
            .AndSkip( Terms.Text( "::" ) )
            .And( Terms.NamespaceIdentifier() )
            .Then<Expression>( static ( _, parts ) =>
                {
                    var (type, key) = parts;

                    if ( key == null )
                        throw new InvalidOperationException( "Key must be specified." );

                    var configKey = key.ToString()!.Replace( '.', ':' );
                    return ExpressionExtensions.ConfigurationValue( type ?? typeof( string ), configKey );
                }
            )
            .Named( "config" );
    }

    public bool CanWrite( Expression node )
    {
        return node is ConfigurationExpression;
    }

    public void WriteExpression( Expression node, ExpressionWriterContext context )
    {
        if ( node is not ConfigurationExpression configurationExpression )
            return;

        using var writer = context.EnterExpression( "Hyperbee.Expressions.ExpressionExtensions.ConfigurationValue", true, false );

        writer.WriteType( configurationExpression.Type );

        if ( configurationExpression.Key != null )
        {
            writer.Write( ",\n" );
            writer.Write( $"\"{configurationExpression.Key}\"", indent: true );
        }
    }

    public void WriteExpression( Expression node, XsWriterContext context )
    {
        if ( node is not ConfigurationExpression configurationExpression )
            return;

        using var writer = context.GetWriter();

        writer.Write( "config<" );
        writer.WriteType( configurationExpression.Type );
        writer.Write( ">" );

        if ( configurationExpression.Key != null )
        {
            writer.Write( "::" );
            writer.Write( configurationExpression.Key.Replace( ':', '.' ) );
        }
    }
}
