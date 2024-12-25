---
layout: default
title: Type System
nav_order: 3
---
# Type System

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
