![Header](MarkDown_Assets/UniInk_ReadmeHeader.jpg)

# ✒️ UniInk - AOT C# Interpreter

![Stars Num](https://img.shields.io/github/stars/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![Forks Num](https://img.shields.io/github/forks/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![License  ](https://img.shields.io/badge/license-MIT-yellow)
![Version  ](https://img.shields.io/badge/version-1.0.0-green)
![C#       ](https://img.shields.io/badge/CSharp-8.0%20+%20-blue)
![Platforms](https://img.shields.io/badge/platforms-Android%20|%20Windows%20|%20(ios)-lightgrey)
---

# 😀Start

### Download [This](https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4AOT/blob/main/Arc.UniInk/Arc.UniInk/UniInk_Speed.cs) is Enough !

# 📝RoadMap

- [ ] Support more features.
- [ ] Nuget Package Support.
- [ ] Unity Package Support.
- [ ] Write more test cases.
- [ ] Serializable function? Maybe interesting?

# ✨Features

- ### The UniInk is a single file, you can download/copy the file and put it in your project.
- ### You can easy to extend the operator and function and more,because the code is clear and simple( only 2000lines )
- ### The performance is very good, the GC is 0 and the boxing is 0.

| Method                                |            Mean |         Error |         StdDev |          Median |     Gen0 |    Gen1 | Allocated |
|---------------------------------------|----------------:|--------------:|---------------:|----------------:|---------:|--------:|----------:|
| TEST_Arithmetic__UniInkSpeed          |   293,666.10 ns |    333.563 ns |     260.424 ns |   293,632.98 ns |        - |       - |      68 B |
| TEST_Arithmetic__UniInkSpeed_Compiled |   175,568.29 ns |  3,454.616 ns |   5,063.732 ns |   176,105.37 ns |        - |       - |     658 B |
| TEST_Arithmetic__ExpressionEvaluator  | 2,622,376.28 ns | 10,903.378 ns |  10,199.026 ns | 2,623,418.75 ns | 390.6250 | 11.7188 | 2068334 B |
| TEST_Arithmetic__Sprache              | 2,797,690.07 ns | 91,953.442 ns | 265,306.706 ns | 2,677,421.88 ns | 535.1563 | 19.5313 | 2817177 B |
| TEST_Arithmetic__ParsecSharp          | 1,114,429.92 ns | 21,943.809 ns |  27,751.892 ns | 1,113,960.74 ns | 162.1094 |       - |  851830 B |

summary : UniInkSpeed is fast , and UniInkSpeed support more features. would you like to try it?

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

// has API to register the function such as CRE:
UniInk_Speed.RegisterFunction("CRE", new(prms =>
{
    var param1 = (InkValue)prms[0];
    var param2 = (InkValue)prms[1];

    return new Card() { ID = param1, Rarity = param2 };
}));

// the result will return the last expression
var res = Ink.Evaluate("var a = CRE(1,3) ;   GET(a, Rarity) == 3 ") as InkValue;


```

- ### ✨Support Lambda Expression

```csharp

// this may be complex, but we also support it
var res = Ink.Evaluate("FLT(Config,var c => GET(c, Rarity) == 2 && GET(c, ID) == 1)") as InkValue;

```

- ### ✨Support Property Getter

```csharp

// use register variable to set getter
UniInk_Speed.RegisterVariable("grower", InkValue.SetGetter(value =>
{
    value.ValueType = TypeCode.Int32;
    value.Value_int = grower++; // grower is a member variable 
}));

// the result is different every time
var res = Ink.Evaluate("grower + 10086") as InkValue;


```

# 💬Support

If you have any questions, suggestions, or need assistance, please feel free
to [open an issue](https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/issues) on the UniInk repository.
We're here to help! 😊

Let's script with UniInk and unlock the full potential of your projects! 🚀💡🌟
