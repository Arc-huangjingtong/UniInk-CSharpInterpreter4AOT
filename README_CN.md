# ✒️ UniInk - AOT C# Interpreter

![Stars Num](https://img.shields.io/github/stars/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![Forks Num](https://img.shields.io/github/forks/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![License  ](https://img.shields.io/badge/license-MIT-yellow)
![Version  ](https://img.shields.io/badge/version-1.0.0-green)
![C#       ](https://img.shields.io/badge/CSharp-8.0%20or%20later-blue)
![Platforms](https://img.shields.io/badge/platforms-Android%20|%20Windows%20|%20(ios)-lightgrey)

---

```diff
! 本仓库正在积极开发中，尚未完成,仅供学习交流
+ 目前UniInk的基本部分已经完成，但是性能还有待优化（性能指标见：UniInk_Tests.cs），欢迎各位大佬体验并提出建议！
+ 另外，本人正在积极开发UniInk的Speed版本，在我的期望中，我希望它能在放弃一些特性的情况下，达到极致的性能(0GC 以及 0运行时反射/少量初始化反射 0拆装箱)
```
# ✨About
- UniInk是一个轻量化的C#脚本解释器,支持C#大部分常用的特性,并且支持解析lambda函数,可以在运行时动态执行C#脚本
- 该项目有一个仅仅两千行的脚本,一个单元测试脚本组成,和一个常用扩展函数脚本组成
- 它非常容易集成到你的项目中,实例化一个解释器,然后调用Evaluate函数即可
- 非常容易自定义你的语法,创作一个自己的脚本语言,项目中有丰富的注释,可以帮助你快速上手
- 什么样的情景适合使用该项目?
  - 本项目的编写初衷,主要是用于编写卡片游戏的技能编辑器,通过简化代码的编写,让策划同学写简单的指令,来实现复杂的技能效果
  - 相比维护复杂的编辑器面板来说,在卡片配置中直接写脚本更加简单,这边的策划也能普遍接受这种方式,如果策划需要新的指令,则在脚本中写一个新的即可
  - 有些时候,技能本身,也需要被抽象,那么抽象成字符串,再解析成脚本,是一个很好的方式
  - 解释器不支持也不想支持类型和结构体的定义,因为这样会导致解释器变得非常复杂,而且不符合初衷,如果你需要这样的功能,请使用Roslyn
  - 脚本的编写主要是对已经封装好的命令的排列组合
- 关于性能:
  - 作者正在积极优化中,目前的测试案例的平均`表达式`执行速度在20ms左右,预计目标是个位数,如果你有更好的优化方案,欢迎提出
  - 很显然,解释表达式所带来的内存和性能开销是非常大的,但同时也无法忽视它的灵活性,省略了很多不必要的抽象,我也相信可以通过后续迭代来优化性能
- 关于后续:
  - 作者不希望制作一个全能的解释器,而是让他可以在特定的场景下发挥作用


# 📝RoadMap

- [x] 自定义函数支持
- [x] 权衡特性支持和性能,C#的关键字和特性过多,为了代码简洁,节选一些特性实现
- [ ] 优化反射性能
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