```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19044.1415/21H2/November2021Update)
Intel Core i7-10700F CPU 2.90GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT


```
| Method    | Mean           | Error      | StdDev     | Gen0   | Allocated |
|---------- |---------------:|-----------:|-----------:|-------:|----------:|
| INT_Limit |      0.0266 ns |  0.0013 ns |  0.0012 ns |      - |         - |
| INT_2     |  5,701.8748 ns | 11.6599 ns | 10.9067 ns |      - |         - |
| INT_3     | 17,580.2824 ns | 32.2734 ns | 28.6095 ns | 1.4648 |    7683 B |
