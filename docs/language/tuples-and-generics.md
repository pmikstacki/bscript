---
layout: default
title: Tuples and Generics
parent: Language
nav_order: 15
---

# Description

Tuples and generics in XS allow you to work with multiple values and generic types.

## Tuples

Tuples are used to store multiple values in a single variable.

```xs
var tuple = (value1, value2, value3);
```

## Generics

Generics allow you to define classes and methods with a placeholder for the type.

```xs
var list = new List<type> { value1, value2, value3 };
```

## Examples

### Tuples

```xs
var tuple = (42, "Hitchhiker's Guide", true);
var first = tuple.Item1;
var second = tuple.Item2;
var third = tuple.Item3;
```

### Generics

```xs
var list = new List<int> { 42, 10, 3 };
foreach (var item in list) {
    item;
}
```
