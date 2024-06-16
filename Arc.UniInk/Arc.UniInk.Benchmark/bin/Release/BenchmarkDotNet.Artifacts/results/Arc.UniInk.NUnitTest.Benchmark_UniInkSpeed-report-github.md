```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
| Method             | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------------------- |----------:|----------:|----------:|-------:|----------:|
| TEST_SCRIPTS_SPEED |  2.895 μs | 0.0058 μs | 0.0052 μs | 0.0114 |      64 B |
| INT_UniInk         | 30.773 μs | 0.0551 μs | 0.0489 μs |      - |      96 B |
