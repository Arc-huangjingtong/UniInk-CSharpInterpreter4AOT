using System;
using System.Collections.Generic;
using System.Reflection;
using Arc.UniInk;

namespace Arc.UniInk.Benchmark
{
    public class UniInk_BestPractices
    {
        // Draft01: Using InkMethodAttribute to register functions
        // Easy to use and maintain

        [InkMethod("TestCommand")]
        public void TestCommand()
        {
            Console.WriteLine("UniInk_BestPractices TestCommand executed.");
        }

        public void Test()
        {
            var ink = new UniInk();
            // This is a placeholder for testing the InkMethodAttribute functionality.
            InkMethodAttribute.RegisterAllInkMethods(ink);

            ink.Evaluate("TestCommand()");
        }


        // Draft02: Easy Evaluate ，Out of the box
        public void EasyEvaluate()
        {
            var ink = new UniInk();
            //var res = ink.Evaluate<int>("1 + 2 * 3 - 4 / 2");

            //Console.WriteLine($"Result: {res}");
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class InkMethodAttribute : Attribute
    {
        public InkMethodAttribute(string methodName)
        {
            MethodName = methodName;
        }

        /// <summary> The function name visible inside UniInk expressions. </summary>
        public string MethodName { get; }

        // global method registry for ink methods
        public static void RegisterAllInkMethods(UniInk uniInk)
        {
            if (uniInk == null) throw new ArgumentNullException(nameof(uniInk));

            // 收集并注册所有带有 InkMethodAttribute 的方法
            var asm = Assembly.GetExecutingAssembly();

            foreach (var type in asm.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                              BindingFlags.Instance);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<InkMethodAttribute>(inherit: true);
                    if (attr == null) continue;

                    if (method.IsGenericMethodDefinition)
                    {
                        throw new NotSupportedException(
                            $"InkMethod does not support generic method: {type.FullName}.{method.Name}");
                    }

                    var inkName = string.IsNullOrWhiteSpace(attr.MethodName) ? method.Name : attr.MethodName;

                    // only support: () or (IList<object>) / (List<object>) for now
                    var parameters = method.GetParameters();
                    var paramMode = parameters.Length switch
                    {
                        0 => 0,
                        1 when typeof(IList<object>).IsAssignableFrom(parameters[0].ParameterType) => 1,
                        _ => -1
                    };

                    if (paramMode == -1)
                    {
                        throw new NotSupportedException(
                            $"InkMethod only supports no-arg or single IList<object> parameter: {type.FullName}.{method.Name}");
                    }

                    // For instance methods, create one instance per declaring type.
                    object instance = null;
                    if (!method.IsStatic)
                    {
                        try
                        {
                            instance = Activator.CreateInstance(type);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Failed to create instance for InkMethod registration: {type.FullName} (need parameterless ctor)",
                                ex);
                        }
                    }

                    uniInk.RegisterFunction(inkName, new InkFunction(args =>
                    {
                        object result;

                        if (paramMode == 0)
                        {
                            result = method.Invoke(instance, null);
                        }
                        else
                        {
                            // UniInk passes List<object>, but accept IList<object>.
                            result = method.Invoke(instance, new object[] { args });
                        }

                        return result;
                    }));
                }
            }
        }
    }
}