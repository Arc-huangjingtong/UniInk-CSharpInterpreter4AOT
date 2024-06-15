```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.2506/23H2/2023Update/SunValley3)
13th Gen Intel Core i5-13400F, 1 CPU, 16 logical and 10 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.8.1 (4.8.9181.0), X86 LegacyJIT


```
<<<<<<< HEAD
| Method             | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------------------- |----------:|----------:|----------:|-------:|----------:|
| INT_UniInk         | 32.725 μs | 0.3136 μs | 0.2934 μs |      - |      96 B |
| TEST_SCRIPTS_SPEED |  2.695 μs | 0.0212 μs | 0.0198 μs | 0.0038 |      32 B |
=======
| Method             | Mean      | Error     | StdDev    | Allocated |
|------------------- |----------:|----------:|----------:|----------:|
| INT_UniInk         | 46.042 μs | 0.0706 μs | 0.0551 μs |         - |
| TEST_SCRIPTS_SPEED |  4.213 μs | 0.0139 μs | 0.0130 μs |         - |
>>>>>>> 431104ad5fbd19fda26cef0501b0878739eefd90
