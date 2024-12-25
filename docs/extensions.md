# Extensions

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



