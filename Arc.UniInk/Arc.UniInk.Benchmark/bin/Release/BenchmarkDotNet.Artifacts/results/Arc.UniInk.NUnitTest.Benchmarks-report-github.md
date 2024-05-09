```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19044.1415/21H2/November2021Update)
Intel Core i7-10700F CPU 2.90GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]       : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  LegacyJitX86 : .NET Framework 4.8 (4.8.4400.0), X86 LegacyJIT
  RyuJitX64    : .NET Framework 4.8 (4.8.4400.0), X64 RyuJIT VectorSize=256


```
| Method     | Job          | Jit       | Platform | Mean          | Error     | StdDev    | Gen0   | Allocated |
|----------- |------------- |---------- |--------- |--------------:|----------:|----------:|-------:|----------:|
| EmptyArray | LegacyJitX86 | LegacyJit | X86      |     0.0000 ns | 0.0000 ns | 0.0000 ns |      - |         - |
| EightBytes | LegacyJitX86 | LegacyJit | X86      |     1.5564 ns | 0.0076 ns | 0.0059 ns | 0.0038 |      20 B |
| SomeLinq   | LegacyJitX86 | LegacyJit | X86      | 1,047.9823 ns | 0.7302 ns | 0.6097 ns | 0.0668 |     357 B |
| EmptyArray | RyuJitX64    | RyuJit    | X64      |     0.2007 ns | 0.0037 ns | 0.0031 ns |      - |         - |
| EightBytes | RyuJitX64    | RyuJit    | X64      |     2.0777 ns | 0.0335 ns | 0.0297 ns | 0.0051 |      32 B |
| SomeLinq   | RyuJitX64    | RyuJit    | X64      | 1,106.7524 ns | 2.5118 ns | 2.2266 ns | 0.0782 |     497 B |
