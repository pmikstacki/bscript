---
layout: default
title: Using Statement
parent: Language
nav_order: 18
---

# Using Statement

The `using` statement ensures that the resource is disposed of when the block of code is exited. 

This construct requires the `Hyperbee.XS.Extensions` package.

## Usage

```xs
using (var resource = new Resource()) {
    resource.DoSomething();
}
```