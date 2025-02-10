---
layout: default
title: Package
parent: Language
nav_order: 12
---

# Package

The `package` directive references, or downloads, a nuget package, allowing you to use its types and methods in your code. 

This construct requires the `Hyperbee.XS.Extensions` package.

## Usage

```xs
package Humanizer.Core:latest;
using Humanizer;
            
var number = 123;
number.ToWords();
```