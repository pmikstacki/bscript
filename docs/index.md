---
layout: default
title: Hyperbee ExpressionScript (XS)
nav_order: 1
---
# e***X***pression***S***cript (***XS***) :rocket:

e***X***pression***S***cript (***XS***) is a minimalist scripting language designed to
simplify c# Expression Trees.

## Summary of Objectives

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

## Credits

Special thanks to:

- [Parlot](https://github.com/sebastienros/parlot) for the performant .NET parser combinator. :heart:
- [Fast Expression Compiler](https://github.com/dadhi/FastExpressionCompiler) for improved performance. :rocket:
- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) 
for more details.

