# e***X***pression***S***cript (***XS***) :rocket:

ExpressionScript (**XS**) is a minimalist scripting language designed to align directly with .NET Expression Trees. 
Its purpose is to simplify the creation, evaluation, and manipulation of expressions by offering a scripting syntax 
that mirrors Expression Tree constructs.

---

## **Introduction to XS**

### **Key Features**

1. **Alignment with Expression Trees**: XS constructs map to corresponding .NET Expression Tree constructs.
2. **Simplified Syntax**: Write expression trees using a clean scripting language.
3. **Extensibility**: Extend the language with custom parsers for specific domain needs.

### **Examples**

**XS Script:**

```plaintext
var a = 10;
var b = 20;
var result = a + b;
```

**Resulting Expression Tree:**

```csharp
Expression.Block(
    new[] { a, b },
    Expression.Assign(a, Expression.Constant(10)),
    Expression.Assign(b, Expression.Constant(20)),
    Expression.Add(a, b)
);
```

---

## **Basic Syntax**

### **Variable Declarations**

```plaintext
var x = 5;
var y = "hello";
var z = true;
```

### **Supported Literals**

XS supports:

- Integers: `42`
- Longs: `42L`
- Floats: `3.14F`
- Doubles: `3.14`
- Characters: `'c'`
- Strings: `"example"`
- Booleans: `true`, `false`
- Null: `null`

---

## **Operators**

### **Assignment Operators**

```plaintext
value = a + b;
value += a;
value -= a;
value *= a;
value /= a;
value ?= y;
```

### **Arithmetic Operators**

```plaintext
var sum = a + b;
var diff = a - b;
var product = a * b;
var quotient = a / b;
```

### **Comparison Operators**

```plaintext
var isEqual = a == b;
var isNotEqual = a != b;
var isGreater = a > b;
var isLesser = a < b;
```

### **Logical Operators**

```plaintext
var andResult = a && b;
var orResult = a || b;
var notResult = !a;
```

### **Prefix and Postfix Operators**

```plaintext
var x = 10;
var postinc = x++;
var postdec = x--;
var preinc = ++x;
var predec = --x;
```

### **Null Coalescing**

```plaintext
var result = a ?? b;
```

### **Casting and Type Checking**

```plaintext
var casted = someValue as int?;
var isInt = someValue is int;
var defaultOrValue = someValue as? int; // Returns default int if cast fails
```

### **String Interpolation**

XS supports string interpolation using backtick-delimited strings.

Example
```plaintext
var value = "world";
var myString = `hello {value}.`;
```

---

## **Control Flow**

### **If Statements**

```plaintext
var result = if (x > 10) {
    x * 2;
} else {
    x / 2;
};
```

### **Switch Statements**

```plaintext
var message = switch (x) {
    case 1: "One";
    case 2: "Two";
    default: "Other";
};
```

---

## **Loops**

```plaintext
var x = 10;
loop 
{
    if (x == 0) 
    {
        break;
    }
    x--;
}
```

---

## **Extensions**

Extensions such as `for`, `foreach`, `while`, `using`, `async/await` are easily added via the `Hyperbee.Xs.Extensions` package. 
For example:

```csharp
var config = new XsConfig { Extensions = Hyperbee.Xs.Extensions.XsExtensions };
var parser = new XsParser( config );
```

### **Async/Await**

```plaintext
async { // Async block
   var result = await SomethingAsync();
}
```

### **For Loop**

```plaintext
for (var i = 0; i < 10; i++) {
    print(i);
}
```

### **Foreach Loop**

```plaintext
foreach (var item in collection) {
    print(item);
}
```

---

## **Exception Handling**

### **Try-Catch**

```plaintext
try {
    var result = riskyOperation();
} catch (Exception ex) {
    print(ex.Message);
}
```

### **Throwing Exceptions**

```plaintext
throw new InvalidOperationException("Something went wrong");
```

---

## **Advanced Features**

### **Method Calls**

```plaintext
var list = new System.Collections.Generic.List<int>();
list.Add(1);
list.Add(2);
list.Add(3);
```

### **Lambdas**

```plaintext
var square = lambda (n) {
    n * n;
};

var result = square(5); // Result: 25
```

### **Object Instantiation**

```plaintext
var obj = new MyClass();
var objWithArgs = new MyClass(arg1, arg2);
```

---

## **Extending XS**

### **Custom Extensions**

You can extend XS by adding new syntax and behaviors through `IParseExtension` and a custom `Expression`. 
We use the [Parlot](https://github.com/sebastienros/parlot) parser combinator library to wire custom 
`Expressions` to scripting syntax.

#### Example: While Extension

Given a custom `Expression` class `WhileExpression`:

```csharp
public class WhileParseExtension : IParseExtension
{
    public ExtensionType Type => ExtensionType.Expression;
    public string Key => "while";

    public Parser<Expression> CreateParser( ExtensionBinder binder )
    {
        var (expression, statement) = binder;

        return
            Between(
                Terms.Char( '(' ),
                expression,
                Terms.Char( ')' )
            )
            .And( statement )
            .Then<Expression>( static parts =>
            {
                var (test, body) = parts;
                return ExpressionExtensions.While( test, body );
            } )
            .Named( "while" );
    }
}
```

#### Wiring Extensions

```csharp
var config = new XsConfig { Extensions = [new WhileParseExtension()] };
var parser = new XsParser( config );
```

#### Using the Extension
```plaintext
var x = 10;

while (x > 0) 
{
    x--;
}
```

---

## **Conclusion**

ExpressionScript (**XS**) offers a concise yet powerful syntax for working with .NET Expression Trees. Its extensibility 
and simplicity make it a great tool for developers who need robust scripting integrated into .NET ecosystems.

## Credits

Special thanks to:

- [Parlot](https://github.com/sebastienros/parlot) for the fast .NET parser combinator. :heart:
- [Fast Expression Compiler](https://github.com/dadhi/FastExpressionCompiler) for improved performance. :rocket:
- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) 
for more details.
