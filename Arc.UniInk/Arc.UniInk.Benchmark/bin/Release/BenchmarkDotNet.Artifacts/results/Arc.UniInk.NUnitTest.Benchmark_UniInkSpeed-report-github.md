```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19044.1415/21H2/November2021Update)
Intel Core i7-10700F CPU 2.90GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT


```
| Method            | Mean           | Error       | StdDev      | Gen0   | Allocated |
|------------------ |---------------:|------------:|------------:|-------:|----------:|
| INT_Limit         |      0.0302 ns |   0.0024 ns |   0.0020 ns |      - |         - |
| INT_UniInkNoSpeed | 77,826.3611 ns | 389.1366 ns | 303.8123 ns | 2.3193 |   12254 B |
| Test              |     28.5271 ns |   0.0291 ns |   0.0258 ns |      - |         - |
| INT_UniInk        | 13,023.5263 ns | 165.1587 ns | 154.4895 ns |      - |         - |
