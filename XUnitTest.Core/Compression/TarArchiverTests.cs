using NewLife.Compression;
using NewLife.Log;
using Xunit;
using System.IO;
using NewLife.IO;

namespace XUnitTest.Compression;

public class TarArchiverTests
{
    private readonly String _testDir;
    private readonly String _outputDir;
    private readonly String _tarFile;

    public TarArchiverTests()
    {
        // 准备测试目录和文件
        _testDir = Path.Combine(Path.GetTempPath(), "TarTest_Source");
        _outputDir = Path.Combine(Path.GetTempPath(), "TarTest_Extract");
        _tarFile = Path.Combine(Path.GetTempPath(), "test.tar");

        // 确保目录存在且为空
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true);
        if (File.Exists(_tarFile)) File.Delete(_tarFile);

        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_outputDir);

        // 创建测试文件
        CreateTestFile(Path.Combine(_testDir, "test1.txt"), "这是测试文件1的内容");
        CreateTestFile(Path.Combine(_testDir, "test2.txt"), "这是测试文件2的内容，包含一些中文字符");

        // 创建子目录和文件
        var subDir = Path.Combine(_testDir, "subdir");
        Directory.CreateDirectory(subDir);
        CreateTestFile(Path.Combine(subDir, "test3.txt"), "子目录中的测试文件");
    }

    private void CreateTestFile(String path, String content)
    {
        File.WriteAllText(path, content);
    }

    [Fact(DisplayName = "测试创建Tar文件")]
    public void TestCreateTar()
    {
        var archiver = new TarArchiver();

        // 创建tar文件
        archiver.CreateTar(_testDir, _tarFile);

        // 验证tar文件已创建
        Assert.True(File.Exists(_tarFile));

        // 验证文件大小大于0
        var fileInfo = new FileInfo(_tarFile);
        Assert.True(fileInfo.Length > 0);

        // 验证文件清单
        var fileList = archiver.FileList;
        Assert.Equal(3, fileList.Count);
        Assert.Contains("test1.txt", fileList);
        Assert.Contains("test2.txt", fileList);
        Assert.Contains("subdir/test3.txt", fileList);
    }

    [Fact(DisplayName = "测试解压Tar文件")]
    public void TestExtractTar()
    {
        var archiver = new TarArchiver();

        // 先创建tar文件
        archiver.CreateTar(_testDir, _tarFile);

        // 解压tar文件
        var extractArchiver = new TarArchiver();
        extractArchiver.ExtractTar(_tarFile, _outputDir);

        // 验证文件已解压
        Assert.True(File.Exists(Path.Combine(_outputDir, "test1.txt")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "test2.txt")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "subdir", "test3.txt")));

        // 验证文件内容
        Assert.Equal("这是测试文件1的内容", File.ReadAllText(Path.Combine(_outputDir, "test1.txt")));
        Assert.Equal("这是测试文件2的内容，包含一些中文字符", File.ReadAllText(Path.Combine(_outputDir, "test2.txt")));
        Assert.Equal("子目录中的测试文件", File.ReadAllText(Path.Combine(_outputDir, "subdir", "test3.txt")));

        // 验证文件清单
        var fileList = extractArchiver.FileList;
        Assert.Equal(3, fileList.Count);
        Assert.Contains("test1.txt", fileList);
        Assert.Contains("test2.txt", fileList);
        Assert.Contains("subdir/test3.txt", fileList);
    }

    [Fact(DisplayName = "测试空目录创建Tar")]
    public void TestCreateTarFromEmptyDirectory()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), "EmptyTarTest");
        if (Directory.Exists(emptyDir)) Directory.Delete(emptyDir, true);
        Directory.CreateDirectory(emptyDir);

        var emptyTarFile = Path.Combine(Path.GetTempPath(), "empty.tar");
        if (File.Exists(emptyTarFile)) File.Delete(emptyTarFile);

        var archiver = new TarArchiver();
        archiver.CreateTar(emptyDir, emptyTarFile);

        // 验证tar文件已创建且文件清单为空
        Assert.True(File.Exists(emptyTarFile));
        Assert.Empty(archiver.FileList);
    }

    [Fact(DisplayName = "测试文件头读写")]
    public void TestTarHeaderReadWrite()
    {
        // 创建一个TarHeader
        var originalHeader = new TarArchiveEntry
        {
            FileName = "test.txt",
            Mode = "0000644",
            OwnerId = "0000000",
            GroupId = "0000000",
            FileSize = 1024,
            LastModified = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Checksum = 0,
            TypeFlag = '0',
            LinkName = String.Empty,
            Magic = "ustar",
            Version = 0,
            OwnerName = String.Empty,
            GroupName = String.Empty,
            DeviceMajor = "0000000\0",
            DeviceMinor = "0000000\0",
            Prefix = String.Empty
        };

        // 写入内存流
        var stream = new MemoryStream();
        originalHeader.Write(stream);

        // 重置流位置以便读取
        stream.Position = 0;

        // 读取头部
        var readHeader = TarArchiveEntry.Read(stream);

        // 验证读取的头部与原始头部一致
        Assert.NotNull(readHeader);
        Assert.Equal(originalHeader.FileName, readHeader.FileName);
        Assert.Equal(originalHeader.FileSize, readHeader.FileSize);
        Assert.Equal(originalHeader.TypeFlag, readHeader.TypeFlag);
        Assert.Equal(originalHeader.Magic, readHeader.Magic);
    }

    [Fact(DisplayName = "测试超长文件名")]
    public void TestLongFileName()
    {
        var longFileName = new String('a', 90) + ".txt";
        var longFilePath = Path.Combine(_testDir, longFileName);

        // 创建带有长文件名的测试文件
        CreateTestFile(longFilePath, "这是一个有很长文件名的测试文件");

        var archiver = new TarArchiver();
        archiver.CreateTar(_testDir, _tarFile);

        // 解压并验证长文件名被正确处理
        var extractArchiver = new TarArchiver();
        extractArchiver.ExtractTar(_tarFile, _outputDir);

        Assert.True(File.Exists(Path.Combine(_outputDir, longFileName)));
        Assert.Contains(longFileName, extractArchiver.FileList);
    }

    [Fact]
    public void TestFiles()
    {
        // 遍历当前目录下的所有tar.gz文件，逐个解析文件头
        foreach (var fi in "../data".AsDirectory().GetFiles("*.tar.gz"))
        {
            XTrace.WriteLine("解析Tar文件：{0}", fi.Name);

            var tar = new TarArchiver(fi.FullName);

            var entries = tar.Entries;

            Assert.NotEmpty(entries);

            foreach (var entry in entries)
            {
                Assert.NotEmpty(entry.FileName);
                Assert.True(entry.FileSize > 0);

                XTrace.WriteLine("\t{0,32}\t{1,10:n0}byte", entry.FileName, entry.FileSize);
            }
        }
    }
}
