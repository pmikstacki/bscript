# ExpressionScript (ES) Language Specification

## 1. Summary of Objectives

ExpressionScript (ES) is a minimalist scripting language designed to:

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

---

## 2. Full Grammar

### ABNF Grammar Specification

```abnf
; Root rule: A script is a sequence of statements
script              = statement *( ";" statement ) [ ";" ]

; Statements
statement           = variable-declaration
                    / assignment
                    / expression
                    / block
                    / loop
                    / try-catch

variable-declaration = "let" identifier "=" expression

assignment          = identifier assignment-operator expression
assignment-operator = "=" / "+=" / "-=" / "*=" / "/="

expression          = literal
                    / identifier
                    / unary-expression
                    / binary-expression
                    / postfix-expression
                    / lambda
                    / method-call
                    / object-instantiation
                    / conditional
                    / switch-expression
                    / block

block               = "{" *( statement ";" ) [ statement ] "}"

; Literals
literal             = integer-literal / float-literal / double-literal
                    / long-literal / short-literal / decimal-literal
                    / string-literal / "null" / "true" / "false"

integer-literal     = DIGIT1 *( DIGIT ) ["n"]
float-literal       = DIGIT1 *( DIGIT ) "." *( DIGIT ) "f"
double-literal      = DIGIT1 *( DIGIT ) "." *( DIGIT ) "d"
long-literal        = DIGIT1 *( DIGIT ) "l"
short-literal       = DIGIT1 *( DIGIT ) "s"
decimal-literal     = DIGIT1 *( DIGIT ) "." *( DIGIT ) "m"
string-literal      = DQUOTE *( %x20-21 / %x23-7E ) DQUOTE

; Unary and Postfix Expressions
unary-expression    = operator expression
operator            = "-" / "!"

postfix-expression  = identifier ( "++" / "--" )

; Binary Expressions
binary-expression   = expression operator expression
operator            = "+" / "-" / "*" / "/" / "%"
                    / "==" / "!=" / ">" / "<" / ">=" / "<="
                    / "??"

; Conditionals
conditional         = "if" "(" expression ")" block [ "else" block ]

; Switch Expression
switch-expression   = "switch" "(" expression ")" "{" case-list "}"
case-list           = *( case-statement )
case-statement      = "case" expression ":" expression ";"
                    / "default" ":" expression ";"

; Loops
loop                = "loop" block
break-statement     = "break"
continue-statement  = "continue"

; Try-Catch
try-catch           = "try" block catch-block
catch-block         = "catch" "(" typename identifier ")" block

; Object Instantiation
object-instantiation = "new" typename "(" [ argument-list ] ")"

; Lambdas
lambda              = "lambda" "(" [ parameter-list ] ")" block
parameter-list      = identifier *( "," identifier )

; Method Calls
method-call         = identifier "." identifier "(" [ argument-list ] ")"
                    / method-call "." identifier "(" [ argument-list ] ")"

argument-list       = expression *( "," expression )

; Identifiers and Typenames
identifier          = ALPHA *( ALPHA / DIGIT / "_" )
typename            = identifier *( "." identifier ) [ generic-args ]
generic-args        = "<" typename *( "," typename ) ">

; Misc
DIGIT               = %x30-39
DIGIT1              = %x31-39
ALPHA               = %x41-5A / %x61-7A
DQUOTE              = %x22
```

---

## 3. Type System

### **Primitive Types**

| Type      | Suffix | Example              |
| --------- | ------ | -------------------- |
| `int`     | `n`    | `let x = 42n`        |
| `float`   | `f`    | `let pi = 3.14f`     |
| `double`  | `d`    | `let e = 2.718d`     |
| `long`    | `l`    | `let big = 10000l`   |
| `short`   | `s`    | `let small = 123s`   |
| `decimal` | `m`    | `let price = 19.99m` |
| `string`  | N/A    | `let name = "hello"` |
| `bool`    | N/A    | `let flag = true`    |
| `null`    | N/A    | `let value = null`   |

### **Type Promotion Rules**

1. Arithmetic operations promote types:
   - `int` → `float` → `double` → `decimal`.
2. Null-coalescing (`??`) resolves to the type of the non-null operand.
3. Conditional expressions (`if-else`) return the common compatible type.

---

## 4. Extensibility

---

## 4. Extensibility

ExpressionScript supports extensibility by allowing developers to define **custom expressions** and seamlessly integrate them into the parser. This provides a clean and flexible way to extend the language for specific use cases or additional functionality.

### **Steps to Add a Custom Expression**

1. **Create the Custom Expression**:
   - Define a class inheriting from `Expression` or extending its existing hierarchy.
2. **Add a Parser Extension Plugin**:
   - Create an object that includes:
     - A method for parsing the new syntax.
     - A factory to generate the custom expression.
   - Register the extension using `Parser.AddExtension()`.
3. **Use the Custom Syntax**:
   - Write scripts using the newly defined syntax.
4. **Generate the Custom Expression Tree**:
   - The custom expression seamlessly integrates into the expression tree pipeline.

---

### **Example: A Custom `RepeatExpression`**

The `RepeatExpression` executes a block of code a specified number of times, similar to a `for` loop.

#### **1. Define the Custom Expression**

Here, we define a `RepeatExpression` class that inherits from `Expression`:

```csharp
public class RepeatExpression : Expression
{
    public Expression Count { get; }
    public Expression Body { get; }

    public RepeatExpression(Expression count, Expression body)
    {
        Count = count;
        Body = body;
    }

    public override ExpressionType NodeType => (ExpressionType)999; // Custom node type
    public override Type Type => typeof(void);
}
```

#### **2. Create the Parser Extension Plugin**

A plugin object includes logic to parse the `repeat` syntax and generate the custom `RepeatExpression`. The extension inherits from `ParserExtension`:

**Base Extension Class:**

```csharp
public abstract class ParserExtension
{
    public abstract bool CanParse(string keyword);
    public abstract Expression Parse(Parser parser);
}
```

**Plugin Implementation:**

```csharp
public class RepeatParserExtension : ParserExtension
{
    public override bool CanParse(string keyword) => keyword == "repeat";

    public override Expression Parse(Parser parser)
    {
        // Expect 'repeat'
        parser.ExpectKeyword("repeat");

        // Parse the count expression in parentheses
        parser.Expect("(");
        var countExpression = parser.ParseExpression();
        parser.Expect(")");

        // Parse the body block
        var body = parser.ParseBlock();

        return new RepeatExpression(countExpression, body);
    }
}

// Extension registration mechanism
public static class ParserExtensions
{
    public static void AddExtension(this Parser parser, ParserExtension extension)
    {
        parser.Extensions.Add(extension);
    }
}
```

#### **3. Register the Custom Expression in the Parser**

The new syntax is registered as a plugin using `Parser.AddExtension()`:

```csharp
var parser = new Parser();
parser.AddExtension(new RepeatParserExtension());
```

#### **4. Use the Custom Expression**

With the parser updated, you can use the `repeat` keyword in your ExpressionScript:

```plaintext
let x = 0;
repeat (5) {
    x++;
}
```

#### **5. Generate the Expression Tree**

The generated expression tree for the above script would look like this:

```csharp
var countParam = Expression.Parameter(typeof(int), "count");
var xParam = Expression.Parameter(typeof(int), "x");
var loopLabel = Expression.Label("RepeatEnd");

var repeatExpression = Expression.Block(
    new[] { xParam, countParam },
    Expression.Assign(xParam, Expression.Constant(0)),
    Expression.Assign(countParam, Expression.Constant(5)),
    Expression.Loop(
        Expression.Block(
            Expression.IfThen(
                Expression.LessThanOrEqual(countParam, Expression.Constant(0)),
                Expression.Break(loopLabel)
            ),
            Expression.PostIncrementAssign(xParam),
            Expression.Decrement(countParam)
        ),
        loopLabel
    )
);
```

---

### **Summary**

By using `Parser.AddExtension()`, developers can:

1. Introduce new syntax into ExpressionScript via plugins.
2. Implement custom expressions like `RepeatExpression`.
3. Keep the parser modular and extensible.
4. Seamlessly integrate the custom expression into the generated expression tree pipeline.

This approach ensures that ExpressionScript remains flexible, extensible, and adaptable to domain-specific needs with minimal effort.

---

## 5. Scripting Examples with Generated Expression Trees

### **Variable Declarations and Assignments**

#### **Script**

```plaintext
let x = 5;
let y = 3.14f;
let z = "hello";
```

#### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { x, y, z },
    Expression.Assign(x, Expression.Constant(5)),
    Expression.Assign(y, Expression.Constant(3.14f)),
    Expression.Assign(z, Expression.Constant("hello"))
);
```

---

### **Postfix Operations and Compound Assignments**

#### **Script**

```plaintext
let x = 5;
x++;
x += 10;
x *= 2;
```

#### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { x },
    Expression.Assign(x, Expression.Constant(5)),
    Expression.PostIncrementAssign(x),
    Expression.AddAssign(x, Expression.Constant(10)),
    Expression.MultiplyAssign(x, Expression.Constant(2))
);
```

---

### **Conditionals and Blocks**

#### **Script**

```plaintext
let result = if (x > 10) {
    x * 2;
} else {
    x - 2;
};
```

#### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { result },
    Expression.Assign(
        result,
        Expression.Condition(
            Expression.GreaterThan(x, Expression.Constant(10)),
            Expression.Multiply(x, Expression.Constant(2)),
            Expression.Subtract(x, Expression.Constant(2))
        )
    )
);
```

---

### **Null-Coalescing Operator**

#### **Script**

```plaintext
let a = null;
let b = 10;
let result = a ?? b;
```

#### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { a, b, result },
    Expression.Assign(a, Expression.Constant(null)),
    Expression.Assign(b, Expression.Constant(10)),
    Expression.Assign(result, Expression.Coalesce(a, b))
);
```

---

### **Loops and Control Flow**

#### **Script**

```plaintext
let x = 10;
loop {
    if (x == 0) {
        break;
    }
    x--;
}
```

#### **Generated Expression Tree**

```csharp
var breakLabel = Expression.Label("BreakLabel");
Expression.Loop(
    Expression.Block(
        Expression.IfThen(
            Expression.Equal(x, Expression.Constant(0)),
            Expression.Break(breakLabel)
        ),
        Expression.PostDecrementAssign(x)
    ),
    breakLabel
);
```

---

### **Method Calls and Method Chaining**

#### **Script**

```plaintext
let list = new System.Collections.Generic.List<int>();
list.Add(1);
list.Add(2);
list.Add(3);

let result = list
    .Where(lambda(n) { n > 1 })
    .Select(lambda(n) { n * n })
    .ToList();
```

#### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { list, result },
    Expression.Assign(list, Expression.New(typeof(List<int>))),
    Expression.Call(list, "Add", null, Expression.Constant(1)),
    Expression.Call(list, "Add", null, Expression.Constant(2)),
    Expression.Call(list, "Add", null, Expression.Constant(3)),
    Expression.Assign(
        result,
        Expression.Call(
            typeof(Enumerable), "ToList", new[] { typeof(int) },
            Expression.Call(
                typeof(Enumerable), "Select", new[] { typeof(int), typeof(int) },
                Expression.Call(
                    typeof(Enumerable), "Where", new[] { typeof(int) },
                    list,
                    Expression.Lambda<Func<int, bool>>(
                        Expression.GreaterThan(Expression.Parameter(typeof(int), "n"), Expression.Constant(1)),
                        Expression.Parameter(typeof(int), "n")
                    )
                ),
                Expression.Lambda<Func<int, int>>(
                    Expression.Multiply(Expression.Parameter(typeof(int), "n"), Expression.Parameter(typeof(int), "n")),
                    Expression.Parameter(typeof(int), "n")
                )
            )
        )
    )
);
```

---

### **Try-Catch**

#### **Script**

```plaintext
try {
    let result = 10 / 0;
} catch (DivideByZeroException e) {
    let message = e.Message;
}
```

#### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { result, message },
    Expression.TryCatch(
        Expression.Block(
            Expression.Assign(result, Expression.Divide(Expression.Constant(10), Expression.Constant(0)))
        ),
        Expression.Catch(
            Expression.Parameter(typeof(DivideByZeroException), "e"),
            Expression.Assign(message, Expression.Property(Expression.Parameter(typeof(DivideByZeroException), "e"), "Message"))
        )
    )
);
```

---

### **Switch Expressions**

#### **Script**

```plaintext
let result = switch (x) {
    case 1: "One";
    case 2: "Two";
    default: "Other";
};
```

#### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { result },
    Expression.Assign(
        result,
        Expression.Switch(
            Expression.Parameter(typeof(int), "x"),
            Expression.Constant("Other"),
            Expression.SwitchCase(Expression.Constant("One"), Expression.Constant(1)),
            Expression.SwitchCase(Expression.Constant("Two"), Expression.Constant(2))
        )
    )
);
```

---

## 6. Conclusion

ExpressionScript provides a clear and concise language for creating, manipulating, and generating expression trees. By directly mapping to `System.Linq.Expressions`, it simplifies the process of dynamic code generation while remaining extensible and expressive for a variety of use cases. Its minimal syntax ensures readability, and its complete support for expression constructs makes it a powerful tool for building dynamic logic.

---
