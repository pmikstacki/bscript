---
layout: default
title: Examples
nav_order: 6
---

# Examples

This section provides various examples of XS code.

## Complex Calculation

```xs
var sum = 0;
for (var i = 0; i < 100; i++) {
    sum += i;
}
return sum;
```

## Nested Expressions

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
result;
```