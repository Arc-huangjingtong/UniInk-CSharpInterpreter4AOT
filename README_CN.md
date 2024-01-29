# ✒️ UniInk - AOT C# Interpreter

![Stars Num](https://img.shields.io/github/stars/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![Forks Num](https://img.shields.io/github/forks/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![License  ](https://img.shields.io/badge/license-MIT-yellow)
![Version  ](https://img.shields.io/badge/version-1.0.0-green)
![C#       ](https://img.shields.io/badge/CSharp-8.0%20or%20later-blue)
![Platforms](https://img.shields.io/badge/platforms-Android%20|%20Windows%20|%20(ios)-lightgrey)

---

```diff
+ 目前UniInk的基本部分已经完成，但是性能还有待优化（性能指标见：UniInk_Tests.cs），欢迎各位大佬体验并提出建议！
+ 另外，本人正在积极开发UniInk的Speed版本，在我的期望中，我希望它能在放弃一些特性的情况下，达到极致的性能(0GC 以及 0运行时反射/少量初始化反射 0拆装箱)
```

# ✨About

- UniInk是一个轻量化的C#脚本解释器,支持C#大部分常用的特性,并且支持解析lambda函数,可以在运行时动态执行C#脚本
- UniInk由一个仅仅两千多行的脚本,一个单元测试脚本组成,非常轻量,可以很方便的集成到你的项目中
- UniInk有着清晰的代码结构,非常容易自定义你的语法,创作一个自己的脚本语言,项目中有丰富的注释,可以帮助你快速上手

# 📦Why Use UniInk?

- 什么样的情景适合使用该项目?
    - 使用HybirdCLR的Unity项目,可以在运行时动态执行简单的指令化的C#脚本,代替原来的Lua.DoString()
    - 以一个较低的成本,用于策划在配置表中实现一些简单的逻辑
    - 轻松实现GM指令,方便维护,定制化程度高
    - 在性能不敏感的场景下,降低维护Unity编辑器面板的成本,将复杂的编辑器面板逻辑,转化为简单的脚本
    - 在重业务的项目场景下,提供抽象逻辑本身的能力,例如策略卡牌的技能系统,在很多时候,技能本身也需要被抽象,那么抽象成字符串,再解析成脚本,是一个很好的方式
- 关于性能:
    - 作者正在积极优化中,目前的测试案例的平均`表达式`执行速度在20ms(包含Log的IO,实际性能在非Update的场景下不慢)
      左右,预计目标是个位数,如果你有更好的优化方案,欢迎提出
    - 目前解释器的性能瓶颈主要在字符串操作,和正则匹配带来的额外GC,反射获取的成员会被缓存,性能损耗可以忽略不计
- 关于后续:
    - 作者不希望实现完成特性的C#解释器,只希望在特定场合,为开发提供便利,

# 📝RoadMap

- [x] 自定义函数支持
- [x] 权衡特性支持和性能,C#的关键字和特性过多,为了代码简洁,节选一些特性实现
- [x] 优化反射性能
- [ ] 高性能模式

# 💡Example Usage

更多案例请查看项目中的单元测试

```csharp
using UniInk;

class Program
{
    static void Main()
    {
        
        UniInk Ink = new()                               // Initialize a new instance;
        int ans1 = Ink.Evaluate<int>("(45 * 2) + 3");    // ans1 = 93;
        int ans2 = Ink.Evaluate<int>("65 > 7 ? 3 : 2");  // ans2 = 3; (supports ternary operators)
        
        object ans = Ink.ScriptEvaluate
            ("if(3>5){return 3;}else if(3==5){return 3;}else{return 5;}"); // ans = 5; (supports ifelse statement)
        object ans = Ink.Evaluate
            ("Avg (1,2,3,4,5,6,7,8,9,10   )"); // ans = 5; (supports custom functions)
        
        Ink.Context= new MyScriptAllAction(); // Set the context; you can use the MyScriptAllAction All members;
        Ink.Evaluate("MyMethod()");           // Evaluate MyMethod Directly;
    }
    
    public class MyScriptAllAction
    {
        public static void MyMethod()
        {
            Console.WriteLine("MyMethod");
        }
    }
    
    
}
```