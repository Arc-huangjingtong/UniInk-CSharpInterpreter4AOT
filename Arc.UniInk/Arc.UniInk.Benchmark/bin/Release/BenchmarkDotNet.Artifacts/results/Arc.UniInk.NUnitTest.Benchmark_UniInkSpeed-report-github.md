```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
| Method     | Mean         | Error     | StdDev    | Gen0   | Allocated |
|----------- |-------------:|----------:|----------:|-------:|----------:|
| INT_UniInk | 12,416.89 ns | 43.700 ns | 36.491 ns | 0.5798 |    3109 B |
| Temp       |     41.17 ns |  0.138 ns |  0.129 ns |      - |         - |
