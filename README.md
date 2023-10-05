# ✒️ UniInk - AOT C# Interpreter

---
![Stars Num](https://img.shields.io/github/stars/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![Forks Num](https://img.shields.io/github/forks/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity?style=social&logo=github)
![License  ](https://img.shields.io/badge/license-MIT-yellow)
![Version  ](https://img.shields.io/badge/version-1.0.0-green)
![C#       ](https://img.shields.io/badge/CSharp-8.0%20or%20later-blue)
![Platforms](https://img.shields.io/badge/platforms-Android%20|%20Windows-lightgrey)
---

```
😮 A single script project and a single scripts NUnit project
✒️ UniInk is a simple C# script interpreter that allows you to effortlessly integrate scripting capabilities into your projects. 🎉
```


## Features ✨

| Feature             | Description                                                                  |
|---------------------|------------------------------------------------------------------------------|
| Easy Integration    | 🧩 Seamlessly integrate UniInk into your C# projects with ease.              |
| Lightweight         | 💪 Lightweight design ensures optimal performance.                           |
| Versatile Scripting | 📝 Supports a wide range of scripting functionalities for dynamic behavior.  |
| Beginner-Friendly   | 🙌 User-friendly interface makes it accessible for beginners.                |
| Extensible          | 🔌 Easily extend UniInk's functionality with custom functions and libraries. |

## Getting Started 🚀

Follow these simple steps to get started with UniInk:

1. Clone the UniInk repository to your local machine.
2. Add the UniInk project to your existing C# solution.
3. Build the solution to ensure all dependencies are resolved.
4. Reference the UniInk library in your desired project.
5. Import the UniInk namespace: `using UniInk;`
6. Initialize an instance of the `ScriptInterpreter` class.
7. Load your script using the `LoadScript` method.
8. Execute the script using the `Execute` method.
9. Enjoy the power of scripting in your project! ✨

## Example Usage 💡

```csharp
using UniInk;

class Program
{
    static void Main()
    {
        ScriptInterpreter interpreter = new ScriptInterpreter();
        
        interpreter.LoadScript("script.uniink");
        interpreter.Execute();
    }
}
```

## Contributing 🤝

Contributions are welcome and encouraged! If you'd like to contribute to UniInk, please follow these steps:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes and commit them with descriptive messages.
4. Push your changes to your forked repository.
5. Submit a pull request, explaining your changes in detail.

## License 📝

UniInk is licensed under the [MIT License](LICENSE). Feel free to use, modify, and distribute this project as per the
terms of the license.

## Support 💬

If you have any questions, suggestions, or need assistance, please feel free
to [open an issue](https://github.com/username/UniInk/issues) on the UniInk repository. We're here to help! 😊

Let's script with UniInk and unlock the full potential of your projects! 🚀💡🌟