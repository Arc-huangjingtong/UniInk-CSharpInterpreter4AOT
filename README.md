![Header](MarkDown_Assets/UniInk_ReadmeHeader.jpg)

# ✒️ UniInk - AOT C# Interpreter


![Stars Num](https://img.shields.io/github/stars/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![Forks Num](https://img.shields.io/github/forks/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![License  ](https://img.shields.io/badge/license-MIT-yellow)
![Version  ](https://img.shields.io/badge/version-1.0.0-green)
![C#       ](https://img.shields.io/badge/CSharp-8.0%20or%20later-blue)
![Platforms](https://img.shields.io/badge/platforms-Android%20|%20Windows%20|%20(ios)-lightgrey)
---


# 📝RoadMap

- [ ] Nuget Package Support.
- [ ] Unity Package Support.
- [ ] Write more test cases.

# ✨Features

## Evaluate Expression

- ### ✨Support Arithmetic Operations ( Easy extend Operator )

```csharp

// 1. Create a new instance of the interpreter 
var Ink = new UniInk_Speed();
// 2. Evaluate the expression , and as it to InkValue
var res1 = Ink.Evaluate(" +9 * (1+1 + 1 + 1 + 1+1+1)") as InkValue;
// 3. Get the result with the type you want, the result is 63
Console.WriteLine(res1.Value_int); 

// PS : the process of calculation is 0 GC and 0 Boxing
//      if you want improve the performance further , recycle the InkValue

InkValue.Release(res1); // now the GC is 0 completely!

```

Other example

```csharp

var Ink = new UniInk_Speed();
// float type
var res = Ink.Evaluate("3333333.3333333f-3.3f+3.3f+  3.3f - 3.3f") as InkValue;
Console.WriteLine(res.Value_float);

// double type
var res2 = Ink.Evaluate("+123456789.987654321d + 987654321.123456789d") as InkValue;
Console.WriteLine(res2.Value_double);

```


- ### ✨Support Logical Operations ( Easy extend Operator )

```csharp

// bool type
var res = Ink.Evaluate("1 < 2 || 2 ==1 || 2 < 1") as InkValue;
Console.WriteLine(res.Value_bool); 

```


## Evaluate Scripts

- ### ✨Support Variable Assignment

```csharp

// the result will return the last expression
var res = Ink.Evaluate("var aaa= 123 ;  var bbb =aaa + 1 ; aaa + bbb ") as InkValue;
Console.WriteLine(res.Value_int); 

```

- ### ✨Support Function Call

```csharp

// the result will return the last expression
// has API to register the function
var res = Ink.Evaluate("var a = CRE(1,3) ;   GET(a, Rarity) == 3 ") as InkValue;


```

- ### ✨Support Lambda Expression

```csharp

// this may be complex, but we also support it
var res = Ink.Evaluate("FLT(Config,var c => GET(c, Rarity) == 2 && GET(c, ID) == 1)") as InkValue;

```



# 💬Support

If you have any questions, suggestions, or need assistance, please feel free
to [open an issue](https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/issues) on the UniInk repository.
We're here to help! 😊

Let's script with UniInk and unlock the full potential of your projects! 🚀💡🌟
