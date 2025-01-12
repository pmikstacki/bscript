---
layout: default
title: Syntax
nav_order: 2
---
# Syntax Examples

## **Examples**

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
