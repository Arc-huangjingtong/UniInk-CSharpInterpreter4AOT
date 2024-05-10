using System;
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