```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19044.1415/21H2/November2021Update)
Intel Core i7-10700F CPU 2.90GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT


```
| Method    | Mean           | Error         | StdDev        | Gen0   | Gen1   | Allocated |
|---------- |---------------:|--------------:|--------------:|-------:|-------:|----------:|
| INT_Limit |      0.0264 ns |     0.0008 ns |     0.0006 ns |      - |      - |         - |
| INT_2     |  5,705.2765 ns |     7.5197 ns |     5.8709 ns |      - |      - |         - |
| INT_3     | 55,509.3162 ns | 1,092.0760 ns | 1,213.8396 ns | 0.5493 | 0.4883 |    3010 B |
| INT_4     | 31,208.8660 ns |   111.3854 ns |   104.1900 ns | 0.6104 | 0.5493 |    3288 B |
