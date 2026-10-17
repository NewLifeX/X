using NewLife.IO;
using Xunit;

namespace XUnitTest.IO;

/// <summary>ObjectInfo 文件/目录信息与 MIME 类型映射测试</summary>
public class ObjectInfoTests
{
    [Fact(DisplayName = "ObjectInfo.FromFile 读取文件名称/大小/时间/类型")]
    public void FromFile_Works()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nl_objectinfo_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "readme.txt");
            File.WriteAllText(path, "hello 新生命");
            var fi = new FileInfo(path);

            var oi = ObjectInfo.FromFile(fi);

            Assert.Equal("readme.txt", oi.Name);
            Assert.Equal(fi.Length, oi.Length);
            Assert.Equal(fi.LastWriteTime, oi.Time);
            Assert.False(oi.IsDirectory);
            Assert.Equal("text/plain", oi.ContentType);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact(DisplayName = "ObjectInfo.FromFile 不存在的文件：大小0、时间最小值")]
    public void FromFile_Missing()
    {
        var fi = new FileInfo(Path.Combine(Path.GetTempPath(), "nl_missing_" + Guid.NewGuid().ToString("N") + ".json"));

        var oi = ObjectInfo.FromFile(fi);

        Assert.Equal(fi.Name, oi.Name);
        Assert.Equal(0, oi.Length);
        Assert.Equal(DateTime.MinValue, oi.Time);
        Assert.False(oi.IsDirectory);
        Assert.Equal("application/json", oi.ContentType);
    }

    [Fact(DisplayName = "ObjectInfo.FromFile 空参数抛 ArgumentNullException")]
    public void FromFile_Null() => Assert.Throws<ArgumentNullException>(() => ObjectInfo.FromFile(null!));

    [Fact(DisplayName = "ObjectInfo.FromDirectory 读取目录名称/时间/标志")]
    public void FromDirectory_Works()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nl_objectdir_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            // 目录内写一个文件，确保 LastWriteTime 有实际值
            File.WriteAllText(Path.Combine(dir, "a.txt"), "a");
            var di = new DirectoryInfo(dir);

            var oi = ObjectInfo.FromDirectory(di);

            Assert.Equal(di.Name, oi.Name);
            Assert.Equal(0, oi.Length);
            Assert.Equal(di.LastWriteTime, oi.Time);
            Assert.True(oi.IsDirectory);
            Assert.Null(oi.ContentType);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact(DisplayName = "ObjectInfo.FromDirectory 不存在的目录：时间最小值")]
    public void FromDirectory_Missing()
    {
        var di = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nl_missing_dir_" + Guid.NewGuid().ToString("N")));

        var oi = ObjectInfo.FromDirectory(di);

        Assert.Equal(di.Name, oi.Name);
        Assert.Equal(0, oi.Length);
        Assert.Equal(DateTime.MinValue, oi.Time);
        Assert.True(oi.IsDirectory);
    }

    [Fact(DisplayName = "ObjectInfo.FromDirectory 空参数抛 ArgumentNullException")]
    public void FromDirectory_Null() => Assert.Throws<ArgumentNullException>(() => ObjectInfo.FromDirectory(null!));

    [Theory(DisplayName = "ObjectInfo.GetContentType 常见扩展名与未知扩展名映射")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".json", "application/json")]
    [InlineData(".png", "image/png")]
    [InlineData(".zip", "application/zip")]
    [InlineData("TXT", "text/plain")]
    [InlineData("json", "application/json")]
    [InlineData(".unknown", "application/octet-stream")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void GetContentType_Works(String? extension, String? expected) => Assert.Equal(expected, ObjectInfo.GetContentType(extension));
}
