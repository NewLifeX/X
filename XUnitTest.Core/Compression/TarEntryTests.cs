using NewLife;
using NewLife.Compression;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Compression;

public class TarEntryTests
{
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

    [Fact]
    public void Test1()
    {
        // 创建一个TarHeader
        var header = new TarEntry
        {
            FileName = Rand.NextString(32),
            FileSize = Rand.Next(),
            TypeFlag = (TarEntryType)Rand.Next(0, 256),
            Magic = Rand.NextString(8),
        };

        // 写入内存流
        var stream = new MemoryStream();
        header.Write(stream);

        // 重置流位置以便读取
        stream.Position = 0;

        // 读取头部
        var header2 = TarEntry.Read(stream);

        // 验证读取的头部与原始头部一致
        Assert.NotNull(header2);
        Assert.Equal(header.FileName, header2.FileName);
        Assert.Equal(header.FileSize, header2.FileSize);
        Assert.Equal(header.TypeFlag, header2.TypeFlag);

        Assert.NotEqual(header.Magic, header2.Magic);
        Assert.Equal(header.Magic[..6], header2.Magic);
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

            var stream = fi.OpenRead().DecompressGZip();

            // 读取头部
            var header = TarEntry.Read(stream);

            Assert.NotNull(header);
            Assert.NotEmpty(header.FileName);
            Assert.True(header.FileSize > 0);
        }
    }
}
