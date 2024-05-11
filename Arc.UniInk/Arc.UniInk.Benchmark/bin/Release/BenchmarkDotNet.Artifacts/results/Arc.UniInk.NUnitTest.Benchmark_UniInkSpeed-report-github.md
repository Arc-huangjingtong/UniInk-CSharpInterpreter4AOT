```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19044.1415/21H2/November2021Update)
Intel Core i7-10700F CPU 2.90GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT


```
| Method    | Mean          | Error      | StdDev    | Allocated |
|---------- |--------------:|-----------:|----------:|----------:|
| INT_Limit |     0.0231 ns |  0.0054 ns | 0.0051 ns |         - |
| INT_2     | 7,192.8256 ns | 11.5645 ns | 9.0288 ns |         - |
