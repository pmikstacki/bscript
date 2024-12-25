---
layout: default
title: Syntax
nav_order: 2
---
# Syntax Examples

## **Variable Declarations and Assignments**

### **Script**

```plaintext
var x = 5;
var y = 3.14f;
var z = "hello";
```

### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { x, y, z },
    Expression.Assign(x, Expression.Constant(5)),
    Expression.Assign(y, Expression.Constant(3.14f)),
    Expression.Assign(z, Expression.Constant("hello"))
);
```

---

## **Postfix Operations and Compound Assignments**

### **Script**

```plaintext
var x = 5;
x++;
x += 10;
x *= 2;
```

### **Generated Expression Tree**

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

## **Conditionals and Blocks**

### **Script**

```plaintext
var result = if (x > 10) 
{
    x * 2;
} 
else 
{
    x - 2;
};
```

### **Generated Expression Tree**

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

## **Null-Coalescing Operator**

### **Script**

```plaintext
var a = null;
var b = 10;
var result = a ?? b;
```

### **Generated Expression Tree**

```csharp
Expression.Block(
    new[] { a, b, result },
    Expression.Assign(a, Expression.Constant(null)),
    Expression.Assign(b, Expression.Constant(10)),
    Expression.Assign(result, Expression.Coalesce(a, b))
);
```

---

## **Loops and Control Flow**

### **Script**

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

### **Generated Expression Tree**

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

## **Method Calls and Method Chaining**

### **Script**

```plaintext
var list = new System.Collections.Generic.List<int>();
list.Add(1);
list.Add(2);
list.Add(3);

var result = list
    .Where(lambda(n) { n > 1 })
    .Select(lambda(n) { n * n })
    .ToList();
```

### **Generated Expression Tree**

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

## **Try-Catch**

### **Script**

```plaintext
try 
{
    var result = 10 / 0;
} 
catch (DivideByZeroException e) 
{
    var message = e.Message;
}
```

### **Generated Expression Tree**

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

## **Switch Expressions**

### **Script**

```plaintext
var result = switch (x) 
{
    case 1: "One";
    case 2: "Two";
    default: "Other";
};
```

### **Generated Expression Tree**

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

