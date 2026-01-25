using NewLife;
using Xunit;

namespace XUnitTest.IO;

[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class PathHelperTests
{
    /// <summary>检查 7z 工具是否可用</summary>
    private static Boolean Is7zAvailable()
    {
        var paths = new[]
        {
            "7z.exe".GetFullPath(),
            "7z/7z.exe".GetFullPath(),
            "../7z/7z.exe".GetFullPath(),
        };
        return paths.Any(File.Exists);
    }

    [Fact]
    public void BasePath()
    {
        var bpath = PathHelper.BasePath;

        Assert.NotEmpty(bpath);
        Assert.Equal(bpath, AppDomain.CurrentDomain.BaseDirectory);

        Assert.Equal("config".GetFullPath(), "config".GetBasePath());

        // 改变
        PathHelper.BasePath = "../xx";
        Assert.Equal("../xx/config".GetFullPath(), "config".GetBasePath());

        PathHelper.BasePath = bpath;
    }

    [Fact]
    public void FileCompress()
    {
        var dst = "xml.zip".AsFile();
        var src = "NewLife.Core.xml".AsFile();

        if (dst.Exists) dst.Delete();

        src.Compress(dst.FullName);

        dst.Refresh();
        Assert.True(dst.Exists);

        var dst2 = "Xml".AsDirectory();
        if (dst2.Exists) dst2.Delete(true);

        dst.Extract(dst2.FullName, true);

        dst2.Refresh();
        Assert.True(dst2.Exists);
    }

    [Fact]
    public void DirectoryCompress()
    {
        var dst = "alg2.zip".AsFile();
        var src = "Algorithms".AsDirectory();

        if (dst.Exists) dst.Delete();

        src.Compress(dst.FullName);

        dst.Refresh();
        Assert.True(dst.Exists);

        var di2 = "Algorithms2".AsDirectory();
        if (di2.Exists) di2.Delete(true);

        dst.Extract(di2.FullName, true);

        di2.Refresh();
        Assert.True(di2.Exists);
    }

    [Fact]
    public void DirectoryCompress3()
    {
        var dst = "alg3.zip".AsFile();
        var src = "Algorithms".AsDirectory();

        if (dst.Exists) dst.Delete();

        src.Compress(dst.FullName, true);

        dst.Refresh();
        Assert.True(dst.Exists);

        var di2 = "Algorithms3".AsDirectory();
        if (di2.Exists) di2.Delete(true);

        dst.Extract(di2.FullName, true);

        di2.Refresh();
        Assert.True(di2.Exists);
    }

    [Theory]
    [InlineData("xml.tar")]
    [InlineData("xml.tar.gz")]
    [InlineData("xml.tgz")]
    public void FileCompressTar(String fileName)
    {
        var dst = fileName.AsFile();
        var src = "NewLife.Core.xml".AsFile();

        if (dst.Exists) dst.Delete();

        src.Compress(dst.FullName);

        dst.Refresh();
        Assert.True(dst.Exists);

        var dst2 = "XmlTar".AsDirectory();
        if (dst2.Exists) dst2.Delete(true);

        dst.Extract(dst2.FullName, true);

        dst2.Refresh();
        Assert.True(dst2.Exists);
    }

    [Theory]
    [InlineData("alg.tar")]
    [InlineData("alg.tar.gz")]
    [InlineData("alg.tgz")]
    public void DirectoryCompressTar(String fileName)
    {
        var dst = fileName.AsFile();
        var src = "Algorithms".AsDirectory();

        if (dst.Exists) dst.Delete();

        src.Compress(dst.FullName);

        dst.Refresh();
        Assert.True(dst.Exists);

        var di2 = "AlgorithmsTar".AsDirectory();
        if (di2.Exists) di2.Delete(true);

        dst.Extract(di2.FullName, true);

        di2.Refresh();
        Assert.True(di2.Exists);
    }

    [Theory]
    [InlineData("xml.7z")]
    public void FileCompress7z(String fileName)
    {
        // 在 CI 环境中跳过，因为没有 7z 工具
        if (!Is7zAvailable())
        {
            return; // Skip test when 7z is not available
        }

        var dst = fileName.AsFile();
        var src = "NewLife.Core.xml".AsFile();

        if (dst.Exists) dst.Delete();

        src.Compress(dst.FullName);

        dst.Refresh();
        Assert.True(dst.Exists);

        var dst2 = "Xml7z".AsDirectory();
        if (dst2.Exists) dst2.Delete(true);

        dst.Extract(dst2.FullName, true);

        dst2.Refresh();
        Assert.True(dst2.Exists);
    }

    [Theory]
    [InlineData("xml.7z")]
    public void DirectoryCompress7z(String fileName)
    {
        // 在 CI 环境中跳过，因为没有 7z 工具
        if (!Is7zAvailable())
        {
            return; // Skip test when 7z is not available
        }

        var dst = fileName.AsFile();
        var src = "Algorithms".AsDirectory();

        if (dst.Exists) dst.Delete();

        src.Compress(dst.FullName);

        dst.Refresh();
        Assert.True(dst.Exists);

        var di2 = "Algorithms7z".AsDirectory();
        if (di2.Exists) di2.Delete(true);

        dst.Extract(di2.FullName, true);

        di2.Refresh();
        Assert.True(di2.Exists);
    }
}