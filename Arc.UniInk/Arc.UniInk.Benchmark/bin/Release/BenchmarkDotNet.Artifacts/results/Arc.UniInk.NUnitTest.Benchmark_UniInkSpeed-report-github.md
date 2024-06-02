```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
| Method             | Mean      | Error     | StdDev    | Allocated |
|------------------- |----------:|----------:|----------:|----------:|
| INT_UniInk         | 30.104 μs | 0.0534 μs | 0.0473 μs |         - |
| TEST_SCRIPTS_SPEED |  2.538 μs | 0.0042 μs | 0.0037 μs |         - |
