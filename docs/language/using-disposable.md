---
layout: default
title: Using Disposable
parent: Language
nav_order: 9
---

## Description

The `using` statement ensures that the resource is disposed of when the block of code is exited. This construct requires the `Hyperbee.XS.Extensions` package.

## Syntax

```abnf
; Using Disposable Resources
using-disposable = "using" "(" declaration ")" block

block = "{" *statement "}"
declaration = "var" identifier [ "=" expression ]
```

## Examples

```xs
using (var resource = new Resource()) {
    resource.DoSomething();
}
```