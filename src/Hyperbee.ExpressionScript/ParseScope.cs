using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Hyperbee.Collections;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS;

public class ParseScope
{
    private readonly Stack<Frame> _frames = new();

    public LinkedDictionary<string, ParameterExpression> Variables = new();
    public Frame Frame => _frames.Peek();

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Push( FrameType frameType, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        var parent = _frames.Count > 0 ? _frames.Peek() : null;
        var frame = new Frame( frameType, parent, breakLabel, continueLabel );

        _frames.Push( frame );
        Variables.Push();
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Pop()
    {
        _frames.Pop();
        Variables.Pop();
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public ParameterExpression LookupVariable( Parlot.TextSpan ident )
    {
        if ( !Variables.TryGetValue( ident.ToString()!, out var variable ) )
            throw new Exception( $"Variable '{ident}' not found." );

        return variable;
    }
}

public enum FrameType
{
    Method,
    Child
}

public class Frame
{
    public FrameType FrameType { get; }
    public Frame Parent { get; }

    public LabelTarget BreakLabel { get; }
    public LabelTarget ContinueLabel { get; }
    public LabelTarget ReturnLabel { get; private set; }

    public Dictionary<string, LabelTarget> Labels { get; } = new();

    public Frame( FrameType frameType, Frame parent = null, LabelTarget breakLabel = null, LabelTarget continueLabel = null )
    {
        FrameType = frameType;
        Parent = parent;
        BreakLabel = breakLabel;
        ContinueLabel = continueLabel;
    }

    public LabelTarget GetOrCreateLabel( string labelName )
    {
        if ( Labels.TryGetValue( labelName, out var label ) )
            return label;

        label = Label( labelName );
        Labels[labelName] = label;

        return label;
    }

    public LabelTarget GetOrCreateReturnLabel( Type returnType )
    {
        var currentFrame = this;

        while ( currentFrame != null )
        {
            if ( currentFrame.FrameType != FrameType.Method )
            {
                currentFrame = currentFrame.Parent;
                continue;
            }

            if ( currentFrame.ReturnLabel == null )
            {
                currentFrame.ReturnLabel = Label( returnType, "ReturnLabel" );
            }
            else if ( currentFrame.ReturnLabel.Type != returnType )
            {
                throw new InvalidOperationException(
                    $"Mismatched return types: Expected {currentFrame.ReturnLabel.Type}, found {returnType}." );
            }

            return currentFrame.ReturnLabel;
        }

        throw new InvalidOperationException( "No enclosing method frame to handle return." );
    }
}
