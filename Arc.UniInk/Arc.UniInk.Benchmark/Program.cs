using System;
using System.Linq;
using System.Linq.Expressions;
using Arc.UniInk.NUnitTest;
using BenchmarkDotNet.Running;


internal class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<Benchmark_UniInkSpeed>();
        Console.WriteLine("Hello World!");
    }
    
}