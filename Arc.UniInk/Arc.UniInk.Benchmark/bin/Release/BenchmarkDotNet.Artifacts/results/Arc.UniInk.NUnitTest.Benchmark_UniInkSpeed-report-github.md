```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
| Method      | Mean            | Error         | StdDev        | Median          | Gen0    | Gen1   | Allocated |
|------------ |----------------:|--------------:|--------------:|----------------:|--------:|-------:|----------:|
| INT_Limit   |       0.0002 ns |     0.0006 ns |     0.0006 ns |       0.0000 ns |       - |      - |         - |
| INT_UniInk  |  11,942.6797 ns |   229.3642 ns |   245.4171 ns |  11,958.2954 ns |  1.1292 |      - |    5957 B |
| INT_Sprache | 210,869.7296 ns | 3,988.1204 ns | 3,916.8674 ns | 209,583.0444 ns | 39.5508 | 0.2441 |  208062 B |
