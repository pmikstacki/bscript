# ExpressionScript (ES) Language Specification

## Summary of Objectives

ExpressionScript (XS) is a minimalist scripting language designed to:

1. **Map directly to `System.Linq.Expressions`**:
   - Every construct in ES aligns conceptually and functionally with `System.Linq.Expressions` APIs.
2. **Simplify Expression Tree Creation**:
   - Avoid manually building expression trees by providing an intuitive scripting interface.
3. **Support All Expression Types**:
   - Fully support constructs like variables, blocks, conditionals, null-coalescing, method calls, loops, and `try-catch`.
4. **Enable Extensibility**:
   - Allow custom expressions and parser extensions to plug into the language seamlessly.
5. **Type Safety**:
   - Provide clear type inference and explicit type promotion rules.
6. **Readable and Writable Expressions**:
   - Allow reverse generation of ES code from existing expression trees.

