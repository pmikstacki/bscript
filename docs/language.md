---
layout: default
title: Language
nav_order: 2
---
# XS Language Reference

This document provides an overview of the XS language's capabilities, including syntax, constructs, and extended functionality.

---

## Supported Literals

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

## Core Constructs

### Variables

```xs
var x = 42;
var y = x + 1;
```

### Conditionals

```xs
var result = if (x > 10) {
    "Greater than 10";
} else {
    "10 or less";
};
```

### Loops

#### Loop

```xs
var x = 0;
loop {
    x++;
    if (x == 10) {
        break;
    }
}
```

#### While Loop (Extended Syntax)

```xs
var x = 0;
while (x < 5) {
    x++;
}
```

#### For Loop (Extended Syntax)

```xs
for (var i = 0; i < 10; i++) {
    print(i);
}
```

#### Foreach Loop (Extended Syntax)

```xs
var list = [1, 2, 3];
foreach (var item in list) {
    print(item);
}
```

### Switch Statements

```xs
var x = 2;
var result = switch (x) {
    case 1: "One";
    case 2: "Two";
    default: "Other";
};
```

### Try-Catch

```xs
try {
    throw new Exception("An error occurred");
} catch (Exception e) {
    print(e.Message);
}
```

### Using Disposable Resources (Extended Syntax)

```xs
using (var resource = new Resource()) {
    resource.DoSomething();
}
```

### String Interpolation

XS supports string interpolation using backtick-delimited strings.

```xs
var value = "world";
var myString = `hello {value}.`;
```

---

## Operators

### Unary Operators

```xs
var isPositive = +x;
var isNegative = -x;
var isNot = !x;
```

### Binary Operators

```xs
var sum = x + y;       // Addition
var diff = x - y;      // Subtraction
var product = x * y;   // Multiplication
var quotient = x / y;  // Division
var remainder = x % y; // Modulo
var power = x ** y;    // Exponentiation
```

### Comparison Operators

```xs
var isEqual = x == y;           // Equality
var isNotEqual = x != y;        // Inequality
var isGreater = x > y;          // Greater than
var isLess = x < y;             // Less than
var isGreaterOrEqual = x >= y;  // Greater than or equal
var isLessOrEqual = x <= y;     // Less than or equal
```

### Logical Operators

```xs
var andResult = a && b;        // Logical AND
var orResult = a || b;         // Logical OR
var notResult = !a;            // Logical NOT
```

### Prefix and Postfix Operators

```xs
var x = 10;
var postinc = x++; // Post-increment
var postdec = x--; // Post-decrement
var preinc = ++x;  // Pre-increment
var predec = --x;  // Pre-decrement
```

### Null Coalescing

```xs
var result = a ?? b; // Returns 'a' if not null, otherwise 'b'
```

### Assignment Operators

```xs
var x = 10;        // Simple assignment
x += 5;            // Addition assignment
x -= 3;            // Subtraction assignment
x *= 2;            // Multiplication assignment
x /= 4;            // Division assignment
x %= 3;            // Modulo assignment
x **= 2;           // Exponentiation assignment
x ?= y;            // Null-coalescing assignment
```

---

## Casting

Casting allows you to convert a value from one type to another.

```xs
var x = (int) 42.5;            // Cast double to int
var y = (string) 123;          // Cast int to string
var z = (double) x / 2;        // Cast int to double during division
var casted = someValue as int?; // Safe cast
var isInt = someValue is int;   // Type check
var defaultOrValue = someValue as? int; // Default if cast fails
```

---

## Method Calls

```xs
var result = myObject.MyMethod(arg1, arg2);
```

---

## Lambdas

```xs
var add = (int x, int y) => x + y;
var result = add(5, 7);
```

---

## Tuples and Generics

### Tuples

```xs
var tuple = (1, "string", true);
var first = tuple.Item1;
```

### Generics

```xs
var list = new List<int> { 1, 2, 3 };
```

---

## Async

### Async Blocks

```xs
async {
    await Task.Delay(1000);
    print("Done");
}
```

### Await Expressions

```xs
var result = await Task.FromResult(42);
```

---

## Debugging with Debug Expressions

### Overview

Debug expressions allow you to inject debugging information directly into your XS scripts. To enable debugging, set the `Debugger` 
property of the `XsConfig` to an action that processes the debug information.

#### Example `XsConfig` Setup

```csharp
var config = new XsConfig
{
    Debugger = (line, column, variables, frame) =>
    {
        Console.WriteLine($"Debugging at Line: {line}, Column: {column}");
        foreach (var kvp in variables)
        {
            Console.WriteLine($"Variable {kvp.Key} = {kvp.Value}");
        }
        Console.WriteLine($"Frame: {frame.FrameType}");
    }
};
```

#### Debugging Syntax

```xs
var x = 42;
debug(x == 42); // Conditional debug
debug();       // Unconditional debug
```

---

## Examples

### Complex Calculation

```xs
var sum = 0;
for (var i = 0; i < 100; i++) {
    sum += i;
}
return sum;
```

### Nested Expressions

```xs
var result = if (x > 10) {
    if (y > 20) {
        "Both conditions met";
    } else {
        "Only x is greater than 10";
    }
} else {
    "x is 10 or less";
};
```

### Using Resources

```xs
using (var resource = new Resource()) {
    resource.Use();
}
```

