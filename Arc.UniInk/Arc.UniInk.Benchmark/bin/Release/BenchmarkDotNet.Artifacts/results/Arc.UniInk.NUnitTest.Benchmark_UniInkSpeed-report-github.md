```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
| Method     | Mean     | Error     | StdDev    | Allocated |
|----------- |---------:|----------:|----------:|----------:|
| Test       |       NA |        NA |        NA |        NA |
| INT_UniInk | 5.193 μs | 0.0237 μs | 0.0222 μs |         - |

Benchmarks with issues:
  Benchmark_UniInkSpeed.Test: DefaultJob
