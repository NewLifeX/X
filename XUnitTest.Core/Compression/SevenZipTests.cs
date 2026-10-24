using System.Reflection;
using NewLife;
using NewLife.Compression;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Compression;

/// <summary>7Zip压缩解压测试</summary>
public class SevenZipTests
{
    private static readonly String _root;
    private static readonly String _testDir;
    private static readonly String _outputDir;
    private static readonly String _archiveFile;

    static SevenZipTests()
    {
        XTrace.WriteLine("SevenZipTests");

        _root = NewLife.Setting.Current.DataPath.GetFullPath();
        _testDir = Path.Combine(_root, "SevenZipTest_Source");
        _outputDir = Path.Combine(_root, "SevenZipTest_Extract");
        _archiveFile = Path.Combine(_root, "test.7z");

        // 确保目录存在且为空
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true);
        if (File.Exists(_archiveFile)) File.Delete(_archiveFile);

        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_outputDir);

        // 创建测试文件
        File.WriteAllText(Path.Combine(_testDir, "file1.txt"), "这是测试文件1的内容 Hello 7Zip");
        File.WriteAllText(Path.Combine(_testDir, "file2.txt"), "这是测试文件2的内容，包含一些中文字符");

        // 创建子目录和文件
        var subDir = Path.Combine(_testDir, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file3.txt"), "子目录中的测试文件");
    }

    /// <summary>7z.exe 缺失时跳过当前用例（CI runner 未必安装 7-Zip），不误判为失败</summary>
    /// <returns>工具是否可用</returns>
    private static Boolean EnsureSevenZip()
    {
        var field = typeof(SevenZip).GetField("_7z", BindingFlags.NonPublic | BindingFlags.Static);
        var path = field?.GetValue(null) as String;
        if (!path.IsNullOrEmpty()) return true;

        XTrace.WriteLine("未找到 7z.exe，跳过当前 SevenZip 用例");

        return false;
    }

    [Fact(DisplayName = "压缩文件到7z")]
    public void CompressTo7z()
    {
        if (!EnsureSevenZip()) return;

        if (File.Exists(_archiveFile)) File.Delete(_archiveFile);

        var sevenZip = new SevenZip();
        sevenZip.Compress(_testDir, _archiveFile);

        Assert.True(File.Exists(_archiveFile));
        Assert.True(new FileInfo(_archiveFile).Length > 0);
    }

    [Fact(DisplayName = "解压7z文件")]
    public void Extract7z()
    {
        if (!EnsureSevenZip()) return;

        // 先确保有压缩文件
        if (!File.Exists(_archiveFile))
        {
            var sevenZip = new SevenZip();
            sevenZip.Compress(_testDir, _archiveFile);
        }

        // 清理输出目录
        if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true);
        Directory.CreateDirectory(_outputDir);

        var sevenZip2 = new SevenZip();
        sevenZip2.Extract(_archiveFile, _outputDir, true);

        // 验证文件已解压
        Assert.True(File.Exists(Path.Combine(_outputDir, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "file2.txt")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "subdir", "file3.txt")));

        // 验证内容
        var content = File.ReadAllText(Path.Combine(_outputDir, "file1.txt"));
        Assert.Contains("Hello 7Zip", content);
    }

    [Fact(DisplayName = "压缩解压往返验证")]
    public void CompressExtractRoundTrip()
    {
        if (!EnsureSevenZip()) return;

        if (File.Exists(_archiveFile)) File.Delete(_archiveFile);
        if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true);
        Directory.CreateDirectory(_outputDir);

        // 压缩
        var sevenZip = new SevenZip();
        sevenZip.Compress(_testDir, _archiveFile);

        Assert.True(File.Exists(_archiveFile));

        // 解压
        var sevenZip2 = new SevenZip();
        sevenZip2.Extract(_archiveFile, _outputDir, true);

        // 验证内容一致
        var original = File.ReadAllText(Path.Combine(_testDir, "file1.txt"));
        var extracted = File.ReadAllText(Path.Combine(_outputDir, "file1.txt"));
        Assert.Equal(original, extracted);
    }

    [Fact(DisplayName = "覆盖模式解压")]
    public void ExtractWithOverwrite()
    {
        if (!EnsureSevenZip()) return;

        if (File.Exists(_archiveFile)) File.Delete(_archiveFile);

        var sevenZip = new SevenZip();
        sevenZip.Compress(_testDir, _archiveFile);

        if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true);
        Directory.CreateDirectory(_outputDir);

        // 先创建同名文件
        File.WriteAllText(Path.Combine(_outputDir, "file1.txt"), "原始内容");

        // 覆盖模式解压
        var sevenZip2 = new SevenZip();
        sevenZip2.Extract(_archiveFile, _outputDir, true);

        // 内容应为压缩包中的内容，而非原始内容
        var content = File.ReadAllText(Path.Combine(_outputDir, "file1.txt"));
        Assert.Contains("Hello 7Zip", content);
    }
}
