// ReSharper disable UnusedAutoPropertyAccessor.Global
#nullable enable
using System;
using System.Collections.Generic;

namespace bscript.Core.Ast;

public readonly record struct SourceSpan(int Offset, int Length, int Line, int Column);

public abstract record AstNode(SourceSpan? Span = null);

public abstract record Expr(SourceSpan? Span = null) : AstNode(Span);

public abstract record Stmt(SourceSpan? Span = null) : AstNode(Span);

// Expressions

public sealed record Literal(object? Value, TypeRef? Type, SourceSpan? Span = null) : Expr(Span);

public sealed record Variable(string Name, TypeRef? StaticType = null, SourceSpan? Span = null) : Expr(Span);

public sealed record TypeRefExpr(TypeRef Type, SourceSpan? Span = null) : Expr(Span);

public sealed record Unary(OpKind Op, Expr Operand, SourceSpan? Span = null) : Expr(Span);

public sealed record Binary(OpKind Op, Expr Left, Expr Right, SourceSpan? Span = null) : Expr(Span);

public sealed record Conditional(Expr Test, Expr Then, Expr Else, SourceSpan? Span = null) : Expr(Span);

public sealed record Lambda(IReadOnlyList<Parameter> Parameters, AstNode Body, SourceSpan? Span = null) : Expr(Span);

public sealed record Invoke(Expr Target, IReadOnlyList<Expr> Args, SourceSpan? Span = null) : Expr(Span);

public sealed record Member(Expr Target, string Name, IReadOnlyList<TypeRef>? TypeArgs, IReadOnlyList<Expr>? Args, SourceSpan? Span = null) : Expr(Span);

public sealed record Index(Expr Target, IReadOnlyList<Expr> Indexes, SourceSpan? Span = null) : Expr(Span);

public sealed record New(TypeRef Type, CtorKind Kind, IReadOnlyList<Expr> Args, IReadOnlyList<Expr>? Initializers = null, SourceSpan? Span = null) : Expr(Span);

// Statements

public sealed record Block(IReadOnlyList<AstNode> Items, SourceSpan? Span = null) : Stmt(Span);

public sealed record Return(Expr? Value, SourceSpan? Span = null) : Stmt(Span);

public sealed record Throw(Expr? Exception, SourceSpan? Span = null) : Stmt(Span);

public sealed record Break(SourceSpan? Span = null) : Stmt(Span);

public sealed record Continue(SourceSpan? Span = null) : Stmt(Span);

public sealed record Goto(string Label, SourceSpan? Span = null) : Stmt(Span);

public sealed record LabelDecl(string Name, SourceSpan? Span = null) : Stmt(Span);

public sealed record If(Expr Test, AstNode Then, AstNode? Else, SourceSpan? Span = null) : Stmt(Span);

public sealed record Loop(AstNode Body, SourceSpan? Span = null) : Stmt(Span);

public sealed record SwitchCase(Expr Test, AstNode Body);

public sealed record Switch(Expr Value, IReadOnlyList<SwitchCase> Cases, AstNode? Default, SourceSpan? Span = null) : Stmt(Span);

public sealed record Catch(Parameter ExceptionVar, AstNode Body);

public sealed record Try(AstNode TryBody, IReadOnlyList<Catch> Catches, AstNode? Finally, SourceSpan? Span = null) : Stmt(Span);

public sealed record Directive(string Text, SourceSpan? Span = null) : Stmt(Span);

// Supporting

public sealed record Parameter(string Name, TypeRef? Type);

public sealed record TypeRef(Type? ClrType, string? Name = null);
