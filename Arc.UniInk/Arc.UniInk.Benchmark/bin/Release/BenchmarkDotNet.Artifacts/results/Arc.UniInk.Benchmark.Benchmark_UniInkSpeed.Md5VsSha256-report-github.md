```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19044.1415/21H2/November2021Update)
Intel Core i7-10700F CPU 2.90GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT


```
| Method | Mean     | Error    | StdDev   |
|------- |---------:|---------:|---------:|
| Sha256 | 77.91 μs | 0.177 μs | 0.166 μs |
| Md5    | 15.41 μs | 0.038 μs | 0.032 μs |
