```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19044.1415/21H2/November2021Update)
Intel Core i7-10700F CPU 2.90GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT


```
| Method             | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------- |---------:|----------:|----------:|-------:|----------:|
| TEST_SCRIPTS_SPEED | 6.902 μs | 0.0158 μs | 0.0148 μs | 0.0076 |      48 B |
