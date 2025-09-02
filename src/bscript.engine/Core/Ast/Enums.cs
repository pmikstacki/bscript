namespace bscript.Core.Ast;

public enum OpKind
{
    // Unary
    Not,
    Negate,
    OnesComplement,
    IsTrue,
    IsFalse,
    Cast,

    // Binary
    Multiply,
    Divide,
    Modulo,
    Add,
    Subtract,
    Power,

    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,

    AndAlso,
    OrElse,
    NullCoalesce,

    BitwiseXor,
    BitwiseAnd,
    BitwiseOr,
    LeftShift,
    RightShift,

    // Type relations
    Is,
    As,

    // Assignment (treated specially by AST or emitter)
    Assign,
    AddAssign,
    SubtractAssign,
    MultiplyAssign,
    DivideAssign,
    ModuloAssign,
    PowerAssign,
    CoalesceAssign,
    XorAssign,
    AndAssign,
    OrAssign,
    LeftShiftAssign,
    RightShiftAssign
}

public enum CtorKind
{
    Object,
    ListInit,
    ArrayBounds,
    ArrayInit
}
