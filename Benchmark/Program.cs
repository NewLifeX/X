using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var config = DefaultConfig.Instance;
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
