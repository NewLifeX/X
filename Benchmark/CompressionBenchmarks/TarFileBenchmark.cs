using BenchmarkDotNet.Attributes;
using NewLife.Compression;

namespace Benchmark.CompressionBenchmarks;

/// <summary>TarFile 读写性能基准</summary>
[MemoryDiagnoser]
[SimpleJob]
public class TarFileBenchmark
{
    private String _workDir = null!;
    private String _dataDir = null!;
    private Byte[] _archiveData = null!;

    [Params(8, 64)]
    public Int32 EntryCount { get; set; }

    [Params(1024)]
    public Int32 PayloadSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _workDir = Path.Combine(Path.GetTempPath(), $"tar-bench-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
        _dataDir = Path.Combine(_workDir, "data");
        Directory.CreateDirectory(_dataDir);

        var random = new Random(20260306);
        for (var i = 0; i < EntryCount; i++)
        {
            var name = i == EntryCount - 1
                ? $"{new String('a', 130)}-{i:D2}.bin"
                : $"f{i:D2}.bin";
            var file = Path.Combine(_dataDir, name);

            var data = new Byte[PayloadSize];
            random.NextBytes(data);
            File.WriteAllBytes(file, data);
        }

        var archiveFile = Path.Combine(_workDir, "sample.tar");
        TarFile.CreateFromDirectory(_dataDir, archiveFile);
        _archiveData = File.ReadAllBytes(archiveFile);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (!String.IsNullOrEmpty(_workDir) && Directory.Exists(_workDir))
            Directory.Delete(_workDir, true);
    }

    [Benchmark(Description = "读取Tar归档")]
    public Int32 ReadTar()
    {
        using var source = new MemoryStream(_archiveData, false);
        var tar = new TarFile();
        tar.Read(source);

        return tar.Entries.Count;
    }

    [Benchmark(Description = "读取并回写Tar归档")]
    public Int64 ReadWriteTar()
    {
        using var source = new MemoryStream(_archiveData, false);
        var tar = new TarFile();
        tar.Read(source);

        using var target = new MemoryStream(_archiveData.Length + 4096);
        tar.Write(target);

        return target.Length;
    }
}
