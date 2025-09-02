using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using bscript.Core;

namespace bscript.Core.Ast;

/// <summary>
/// Converts AST nodes into System.Linq.Expressions for backward compatibility.
/// This is a temporary bridge during migration. Prefer using the AST directly.
/// </summary>
public sealed class ExpressionEmitter
{
    public ITypeResolver TypeResolver { get; }

    public ExpressionEmitter(ITypeResolver typeResolver)
    {
        TypeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
    }

    public Expression Emit(AstNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        return node switch
        {
            Literal lit => EmitLiteral(lit),
            Unary u => EmitUnary(u),
            Binary b => EmitBinary(b),
            Conditional c => EmitConditional(c),
            TypeRefExpr t => EmitTypeRefExpr(t),
            // The following are not yet implemented in the bridge. They will be added incrementally.
            Variable => throw new NotSupportedException("Variable emission requires scope binding and is not implemented yet."),
            Lambda => throw new NotSupportedException("Lambda emission not implemented yet."),
            Invoke => throw new NotSupportedException("Invoke emission not implemented yet."),
            Member => throw new NotSupportedException("Member emission not implemented yet."),
            Index => throw new NotSupportedException("Index emission not implemented yet."),
            New => throw new NotSupportedException("New emission not implemented yet."),
            Block => throw new NotSupportedException("Block emission not implemented yet."),
            Return => throw new NotSupportedException("Return emission not implemented yet."),
            Throw => throw new NotSupportedException("Throw emission not implemented yet."),
            Break => throw new NotSupportedException("Break emission not implemented yet."),
            Continue => throw new NotSupportedException("Continue emission not implemented yet."),
            Goto => throw new NotSupportedException("Goto emission not implemented yet."),
            LabelDecl => throw new NotSupportedException("LabelDecl emission not implemented yet."),
            If => throw new NotSupportedException("If emission not implemented yet."),
            Loop => throw new NotSupportedException("Loop emission not implemented yet."),
            Switch => throw new NotSupportedException("Switch emission not implemented yet."),
            Try => throw new NotSupportedException("Try emission not implemented yet."),
            Directive => Expression.Empty(),
            _ => throw new NotSupportedException($"Unsupported AST node type: {node.GetType().Name}")
        };
    }

    private Expression EmitLiteral(Literal lit)
    {
        var type = lit.Type?.ClrType ?? lit.Value?.GetType() ?? typeof(object);
        return Expression.Constant(lit.Value, type);
    }

    private Expression EmitTypeRefExpr(TypeRefExpr t)
    {
        var type = t.Type?.ClrType ?? throw new InvalidOperationException("TypeRef must have a resolved CLR type for emission.");
        return Expression.Constant(type, typeof(Type));
    }

    private Expression EmitUnary(Unary u)
    {
        var operand = EnsureValue(Emit(u.Operand));
        return u.Op switch
        {
            OpKind.Not => Expression.Not(operand),
            OpKind.Negate => Expression.Negate(operand),
            OpKind.OnesComplement => Expression.OnesComplement(operand),
            OpKind.IsTrue => Expression.IsTrue(operand),
            OpKind.IsFalse => Expression.IsFalse(operand),
            OpKind.Cast => throw new NotSupportedException("Cast emission requires target type and is not implemented here."),
            _ => throw new NotSupportedException($"Unsupported unary operator: {u.Op}")
        };
    }

    private Expression EmitBinary(Binary b)
    {
        var left = EnsureValue(Emit(b.Left));
        var right = EnsureValue(Emit(b.Right));

        return b.Op switch
        {
            OpKind.Multiply => Expression.Multiply(left, right),
            OpKind.Divide => Expression.Divide(left, right),
            OpKind.Modulo => Expression.Modulo(left, right),
            OpKind.Add => Expression.Add(left, right),
            OpKind.Subtract => Expression.Subtract(left, right),
            OpKind.Power => Expression.Power(left, right),

            OpKind.Equal => Expression.Equal(left, right),
            OpKind.NotEqual => Expression.NotEqual(left, right),
            OpKind.LessThan => Expression.LessThan(left, right),
            OpKind.GreaterThan => Expression.GreaterThan(left, right),
            OpKind.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
            OpKind.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),

            OpKind.AndAlso => Expression.AndAlso(left, right),
            OpKind.OrElse => Expression.OrElse(left, right),
            OpKind.NullCoalesce => Expression.Coalesce(left, right),

            OpKind.BitwiseXor => Expression.ExclusiveOr(left, right),
            OpKind.BitwiseAnd => Expression.And(left, right),
            OpKind.BitwiseOr => Expression.Or(left, right),
            OpKind.LeftShift => Expression.LeftShift(left, right),
            OpKind.RightShift => Expression.RightShift(left, right),

            // Assignments and type relations need special handling; not implemented in skeleton
            OpKind.Assign or
            OpKind.AddAssign or
            OpKind.SubtractAssign or
            OpKind.MultiplyAssign or
            OpKind.DivideAssign or
            OpKind.ModuloAssign or
            OpKind.PowerAssign or
            OpKind.CoalesceAssign or
            OpKind.XorAssign or
            OpKind.AndAssign or
            OpKind.OrAssign or
            OpKind.LeftShiftAssign or
            OpKind.RightShiftAssign or
            OpKind.Is or
            OpKind.As => throw new NotSupportedException($"Operator not implemented in emitter: {b.Op}"),

            _ => throw new NotSupportedException($"Unsupported binary operator: {b.Op}")
        };
    }

    private Expression EmitConditional(Conditional c)
    {
        var test = EnsureValue(Emit(c.Test));
        var ifTrue = EnsureValue(Emit(c.Then));
        var ifFalse = EnsureValue(Emit(c.Else));
        return Expression.Condition(test, ifTrue, ifFalse);
    }

    private static Expression EnsureValue(Expression e) => e is UnaryExpression u && u.NodeType == ExpressionType.Convert ? u : e;
}
