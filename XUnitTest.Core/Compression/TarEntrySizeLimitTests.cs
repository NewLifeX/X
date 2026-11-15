using System.IO.Compression;
using NewLife;
using NewLife.Compression;
using Xunit;

namespace XUnitTest.Compression;

/// <summary>tar.gz 条目大小上限：正常大文件可读，超过上限才拒绝</summary>
public class TarEntrySizeLimitTests
{
    private static String NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "TarSize_" + Environment.ProcessId + "_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);

        return root;
    }

    /// <summary>生成指定长度的随机文件并打成 tar.gz，返回压缩包路径</summary>
    private static String CreateTarGz(String root, String name, Int32 length)
    {
        var src = Path.Combine(root, name);
        var data = new Byte[length];
        new Random(length).NextBytes(data);
        File.WriteAllBytes(src, data);

        var tarPath = Path.Combine(root, "a.tar");
        var tar = new TarFile();
        tar.CreateEntryFromFile(src, name);
        using (var fs = File.Create(tarPath)) tar.Write(fs);

        // 不可定位流（tar.gz）才会走「整包读进内存」分支，上限判断在那里
        var gzPath = tarPath + ".gz";
        using (var input = File.OpenRead(tarPath))
        using (var output = File.Create(gzPath))
        using (var gz = new GZipStream(output, CompressionLevel.Fastest))
        {
            input.CopyTo(gz);
        }

        return gzPath;
    }

    [Fact(DisplayName = "tar.gz 中的 2MB 条目仍可读取并解压")]
    public void LargeEntryOverGzipIsReadable()
    {
        var root = NewRoot();
        try
        {
            const Int32 size = 2 * 1024 * 1024;
            var gzPath = CreateTarGz(root, "big.bin", size);

            using var tf = new TarFile();
            using (var fs = File.OpenRead(gzPath))
            using (var gz = new GZipStream(fs, CompressionMode.Decompress))
            {
                tf.Read(gz);
            }

            var entry = Assert.Single(tf.Entries);
            Assert.Equal(size, entry.FileSize);

            var outDir = Path.Combine(root, "out");
            tf.ExtractToDirectory(outDir);
            Assert.Equal(size, new FileInfo(Path.Combine(outDir, "big.bin")).Length);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact(DisplayName = "条目超过 MaxEntrySize 时被拒绝")]
    public void EntryOverLimitIsRejected()
    {
        var root = NewRoot();
        try
        {
            var gzPath = CreateTarGz(root, "big.bin", 1024 * 1024);

            using var tf = new TarFile { MaxEntrySize = 512 * 1024 };
            using var fs = File.OpenRead(gzPath);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);

            Assert.Throws<XException>(() => tf.Read(gz));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
