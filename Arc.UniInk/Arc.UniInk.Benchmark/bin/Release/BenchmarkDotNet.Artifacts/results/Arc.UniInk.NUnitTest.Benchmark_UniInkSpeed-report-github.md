```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
| Method    | Mean           | Error       | StdDev      | Gen0    | Gen1   | Allocated |
|---------- |---------------:|------------:|------------:|--------:|-------:|----------:|
| INT_Limit |      0.0000 ns |   0.0000 ns |   0.0000 ns |       - |      - |         - |
| INT_2     |  3,799.9926 ns |   5.8915 ns |   4.9197 ns |       - |      - |         - |
| INT_3     | 12,759.1044 ns |  48.8709 ns |  45.7139 ns |  1.4648 |      - |    7683 B |
| Main4     | 33,267.7189 ns | 248.5775 ns | 220.3575 ns | 12.0239 | 0.0610 |   63315 B |
