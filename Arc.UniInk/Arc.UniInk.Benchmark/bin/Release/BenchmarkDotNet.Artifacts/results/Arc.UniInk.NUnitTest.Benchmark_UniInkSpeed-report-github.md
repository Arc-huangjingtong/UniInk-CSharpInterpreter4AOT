```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
| Method            | Mean           | Error       | StdDev      | Gen0   | Allocated |
|------------------ |---------------:|------------:|------------:|-------:|----------:|
| INT_Limit         |      0.0221 ns |   0.0056 ns |   0.0053 ns |      - |         - |
| INT_UniInkNoSpeed | 62,333.0037 ns | 191.6202 ns | 179.2417 ns | 2.3193 |   12254 B |
| INT_UniInk        |  9,991.5684 ns |  30.8390 ns |  28.8468 ns |      - |         - |
