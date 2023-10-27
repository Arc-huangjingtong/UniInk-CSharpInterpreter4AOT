# ✒️ UniInk - AOT C# Interpreter

![Stars Num](https://img.shields.io/github/stars/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![Forks Num](https://img.shields.io/github/forks/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![License  ](https://img.shields.io/badge/license-MIT-yellow)
![Version  ](https://img.shields.io/badge/version-1.0.0-green)
![C#       ](https://img.shields.io/badge/CSharp-8.0%20or%20later-blue)
![Platforms](https://img.shields.io/badge/platforms-Android%20|%20Windows%20|%20(ios)-lightgrey)
---

```diff
! This repository is a work in progress and is not yet complete.
```

# 📝RoadMap

- [x] Add support for custom functions.
- [x] Delete some useless features.
- [ ] Zero reflection model
- [ ] Optimize reflection performance.
- [ ] Add high performance model.

# ✨Features

| Feature           | Description                                                                  |
|-------------------|------------------------------------------------------------------------------|
| Single Script     | 🧩 Seamlessly integrate UniInk into your C# projects with ease.              |
| Light Weight      | 💪 Lightweight design ensures optimal performance.                           |
| Rich Feature      | 📝 Supports a wide range of scripting functionalities for dynamic behavior.  |
| Beginner-Friendly | 🙌 User-friendly Comment makes it accessible for beginners.                  |
| Extensible        | 🔌 Easily extend UniInk's functionality with custom functions and libraries. |


# 💡Example Usage

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

# 📝License

UniInk is licensed under the [MIT License](LICENSE). Feel free to use, modify, and distribute this project as per the
terms of the license.

# 💬Support

If you have any questions, suggestions, or need assistance, please feel free
to [open an issue](https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/issues) on the UniInk repository.
We're here to help! 😊

Let's script with UniInk and unlock the full potential of your projects! 🚀💡🌟
