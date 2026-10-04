using System.Text;
using NewLife;
using NewLife.Compression;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Compression;

//[CollectionDefinition("TarArchiver", DisableParallelization = true)]
//public class TarArchiverTestsCollection
//{
//    // 这个类不需要包含任何代码
//    // 它的作用是定义一个测试集合，并禁用并行化
//}

//[Collection("TarArchiver")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class TarFileTests
{
    private static String _testDir;
    private static String _outputDir;
    private static String _tarFile;

    static TarFileTests()
    {
        XTrace.WriteLine("TarArchiverTests");

        var root = NewLife.Setting.Current.DataPath.GetFullPath();

        // 准备测试目录和文件
        _testDir = Path.Combine(root, "TarTest_Source");
        _outputDir = Path.Combine(root, "TarTest_Extract");
        _tarFile = Path.Combine(root, "test.tar");

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

    static void CreateTestFile(String path, String content)
    {
        File.WriteAllText(path, content);
    }

    [Fact(DisplayName = "测试创建Tar文件")]
    public void TestCreateTar()
    {
        if (File.Exists(_tarFile)) File.Delete(_tarFile);
        using var archiver = new TarFile(_tarFile, true);

        // 创建tar文件
        archiver.CreateFromDirectory(_testDir);

        // 验证tar文件已创建
        Assert.True(File.Exists(_tarFile));

        // 验证文件大小大于0
        var fileInfo = new FileInfo(_tarFile);
        Assert.True(fileInfo.Length > 0);

        // 验证文件清单
        var fileList = archiver.Entries.Select(e => e.FileName).ToList();
        Assert.Equal(3, fileList.Count);
        Assert.Contains("test1.txt", fileList);
        Assert.Contains("test2.txt", fileList);
        Assert.Contains("subdir/test3.txt", fileList);
    }

    [Fact(DisplayName = "测试解压Tar文件")]
    public void TestExtractTar()
    {
        if (File.Exists(_tarFile)) File.Delete(_tarFile);
        {
            using var archiver = new TarFile(_tarFile, true);

            // 先创建tar文件
            archiver.CreateFromDirectory(_testDir);
        }

        // 解压tar文件
        using var extractArchiver = new TarFile(_tarFile);
        extractArchiver.ExtractToDirectory(_outputDir, true);

        // 验证文件已解压
        Assert.True(File.Exists(Path.Combine(_outputDir, "test1.txt")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "test2.txt")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "subdir", "test3.txt")));

        // 验证文件内容
        Assert.Equal("这是测试文件1的内容", File.ReadAllText(Path.Combine(_outputDir, "test1.txt")));
        Assert.Equal("这是测试文件2的内容，包含一些中文字符", File.ReadAllText(Path.Combine(_outputDir, "test2.txt")));
        Assert.Equal("子目录中的测试文件", File.ReadAllText(Path.Combine(_outputDir, "subdir", "test3.txt")));

        // 验证文件清单
        var fileList = extractArchiver.Entries.Select(e => e.FileName).ToList();
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

        using var archiver = new TarFile(emptyTarFile, true);
        archiver.CreateFromDirectory(emptyDir);

        // 验证tar文件已创建且文件清单为空
        Assert.True(File.Exists(emptyTarFile));
        Assert.Empty(archiver.Entries);
    }

    [Fact(DisplayName = "测试文件头读写")]
    public void TestTarHeaderReadWrite()
    {
        // 创建一个TarHeader
        var originalHeader = new TarEntry
        {
            FileName = "test.txt",
            Mode = "0000644",
            OwnerId = "0000000",
            GroupId = "0000000",
            FileSize = 1024,
            LastModified = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Checksum = 0,
            TypeFlag = TarEntryType.RegularFile,
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
        var readHeader = TarEntry.Read(stream);

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

        if (File.Exists(_tarFile)) File.Delete(_tarFile);
        using var archiver = new TarFile(_tarFile, true);
        archiver.CreateFromDirectory(_testDir);
        archiver.Dispose();

        // 解压并验证长文件名被正确处理
        using var extractArchiver = new TarFile(_tarFile);
        extractArchiver.ExtractToDirectory(_outputDir);

        Assert.True(File.Exists(Path.Combine(_outputDir, longFileName)));
        Assert.Contains(longFileName, extractArchiver.Entries.Select(e => e.FileName).ToList());
    }

    [Fact(DisplayName = "测试超长文件名2")]
    public void TestLongFileName2()
    {
        // 手工构造 GNU 格式长文件名归档（GNU tar 产出形态），验证外部归档的读取兼容性
        var gnuFileName = new String('g', 190) + ".txt";
        var gnuContent = "这是一个有很长文件名的测试文件";
        var gnuFile = _testDir.CombinePath("../test_gnu.tar");
        WriteGnuLongNameArchive(gnuFile, gnuFileName, gnuContent);

        using (var tar = new TarFile(gnuFile))
        {
            Assert.Single(tar.Entries);

            var entry = tar.Entries.First();
            Assert.Equal(gnuFileName, entry.FileName);
            Assert.Equal(gnuContent, entry.Open().ReadBytes(entry.FileSize).ToStr());
        }

        // 我写我读：归档 190 字符长文件名后解包
        var longFileName = new String('a', 190) + ".txt";
        var longFilePath = Path.Combine(_testDir, longFileName);

        // 创建带有长文件名的测试文件
        CreateTestFile(longFilePath, "这是一个有很长文件名的测试文件");

        if (File.Exists(_tarFile)) File.Delete(_tarFile);
        using var archiver = new TarFile(_tarFile, true);
        archiver.CreateFromDirectory(_testDir);
        archiver.Dispose();

        // 解压并验证长文件名被正确处理
        using var extractArchiver = new TarFile(_tarFile);
        extractArchiver.ExtractToDirectory(_outputDir);

        Assert.True(File.Exists(Path.Combine(_outputDir, longFileName)));
        Assert.Contains(longFileName, extractArchiver.Entries.Select(e => e.FileName).ToList());
    }

    /// <summary>手工构造 GNU 格式长文件名归档：长名元数据条目（类型 'L'）后跟真实文件头与内容。</summary>
    /// <param name="path">归档文件路径</param>
    /// <param name="fileName">超过 100 字节的长文件名</param>
    /// <param name="content">文件内容</param>
    private static void WriteGnuLongNameArchive(String path, String fileName, String content)
    {
        var name = Encoding.ASCII.GetBytes(fileName);
        var data = Encoding.UTF8.GetBytes(content);

        using var fs = File.Create(path);

        // 长名元数据条目，名称固定为 ././@LongLink，数据区存放真实文件名
        WriteGnuHeader(fs, "././@LongLink", (Byte)'L', name.Length);
        fs.Write(name, 0, name.Length);
        WriteGnuPadding(fs, name.Length);

        // 真实文件头的名称被截断，读取方以元数据条目的长名为准
        WriteGnuHeader(fs, fileName[..100], (Byte)'0', data.Length);
        fs.Write(data, 0, data.Length);
        WriteGnuPadding(fs, data.Length);

        // 两个空块作为归档结束标志
        fs.Write(new Byte[1024], 0, 1024);
    }

    private static void WriteGnuPadding(Stream stream, Int32 length)
    {
        var padding = (512 - length % 512) % 512;
        if (padding > 0) stream.Write(new Byte[padding], 0, padding);
    }

    private static void WriteGnuHeader(Stream stream, String name, Byte type, Int32 size)
    {
        var header = new Byte[512];
        Encoding.ASCII.GetBytes(name, 0, name.Length, header, 0);
        Encoding.ASCII.GetBytes("0000644\0", 0, 8, header, 100);
        Encoding.ASCII.GetBytes("0000000\0", 0, 8, header, 108);
        Encoding.ASCII.GetBytes("0000000\0", 0, 8, header, 116);
        WriteGnuOctal(header, 124, 12, size);
        WriteGnuOctal(header, 136, 12, 0);
        Array.Fill(header, (Byte)' ', 148, 8); // 校验和区域先填空格再累加
        header[156] = type;
        Encoding.ASCII.GetBytes("ustar\0", 0, 6, header, 257);
        Encoding.ASCII.GetBytes("00", 0, 2, header, 263);

        // 校验和：按空格参与累加，写入 6 位八进制 + NUL + 空格
        Int64 sum = 0;
        foreach (var item in header) sum += item;
        WriteGnuOctal(header, 148, 7, sum);
        header[155] = (Byte)' ';

        stream.Write(header, 0, 512);
    }

    private static void WriteGnuOctal(Byte[] header, Int32 offset, Int32 length, Int64 value)
    {
        // 八进制数字右对齐，前置零填充，末字节留 NUL
        for (var i = offset + length - 2; i >= offset; i--)
        {
            header[i] = (Byte)('0' + (value & 0x07));
            value >>= 3;
        }
        header[offset + length - 1] = 0;
    }

    [Fact]
    public void TestFiles()
    {
        var di = "../data".AsDirectory();
        if (!di.Exists) return;

        // 遍历当前目录下的所有tar.gz文件，逐个解析文件头
        foreach (var fi in di.GetFiles("*.tar.gz"))
        {
            XTrace.WriteLine("解析Tar文件：{0}", fi.Name);

            using var tar = new TarFile(fi.FullName);

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
