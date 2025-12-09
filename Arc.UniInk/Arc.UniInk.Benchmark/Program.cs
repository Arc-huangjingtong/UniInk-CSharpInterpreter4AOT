using Arc.UniInk.Benchmark;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;


var job = Job.Default
    .WithGcServer(false)
    .WithToolchain(InProcessEmitToolchain.Instance); // Use InProcess toolchain to avoid launching a separate process

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddJob(job)
    .AddExporter(MarkdownExporter.Default);


BenchmarkRunner.Run<UniInkBenchmark>(config);