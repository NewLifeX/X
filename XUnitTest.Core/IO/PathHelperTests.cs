using System;
using System.IO;
using System.Linq;
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

    [Fact(DisplayName = "CopyTo 应该复制目录内所有文件并保持相对路径")]
    public void CopyTo_CopiesFilesWithRelativePaths()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "NewLife_Test_CopyTo_" + Guid.NewGuid().ToString("N"));
        var src = Path.Combine(tmp, "src");
        var dst = Path.Combine(tmp, "dst");

        try
        {
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);

            // 创建文件和子目录
            var f1 = Path.Combine(src, "a.txt");
            File.WriteAllText(f1, "hello1");

            var sub = Path.Combine(src, "sub");
            Directory.CreateDirectory(sub);
            var f2 = Path.Combine(sub, "b.txt");
            File.WriteAllText(f2, "hello2");

            var di = new DirectoryInfo(src);
            var res = di.CopyTo(dst, null, true);

            // 断言返回的目标路径存在且数量为2
            Assert.NotNull(res);
            Assert.Equal(2, res.Length);

            foreach (var r in res)
            {
                Assert.True(File.Exists(r), $"目标文件不存在: {r}");
                // 目标路径必须在 dst 根下
                Assert.StartsWith(Path.GetFullPath(dst).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(r), StringComparison.OrdinalIgnoreCase);
            }

            // 内容验证
            var dst1 = Path.Combine(dst, "a.txt");
            var dst2 = Path.Combine(dst, "sub", "b.txt");
            Assert.Equal("hello1", File.ReadAllText(dst1));
            Assert.Equal("hello2", File.ReadAllText(dst2));
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { }
        }
    }

    [Fact(DisplayName = "CopyTo 不应被 TrimStart 风格的错误截断影响（示例 C:/proj）")]
    public void CopyTo_DoesNotManglePath_WhenRootContainsChars()
    {
        // 演示如果使用 TrimStart(root.ToCharArray()) 对字符串做前缀移除，会把字符集当作集合，导致错误截断
        var simRoot = "C:/proj";
        var simFull = "C:/proj/projDir/1.txt";
        var broken = simFull.TrimStart(simRoot.ToCharArray());
        Assert.Equal("Dir/1.txt", broken);

        // 真实文件系统测试，确保我们的 CopyTo 不会产生上述错误
        var tmp = Path.Combine(Path.GetTempPath(), "NewLife_Test_CopyToPrefix2_" + Guid.NewGuid().ToString("N"));
        var src = Path.Combine(tmp, "C_proj");
        var dst = Path.Combine(tmp, "dst");

        try
        {
            var nested = Path.Combine(src, "projDir");
            Directory.CreateDirectory(nested);
            Directory.CreateDirectory(dst);

            var f = Path.Combine(nested, "1.txt");
            File.WriteAllText(f, "ok");

            var di = new DirectoryInfo(src);
            var res = di.CopyTo(dst, null, true);

            // 找到目标文件路径
            var expectedRel = Path.Combine("projDir", "1.txt");
            var found = res.FirstOrDefault(p => p.EndsWith(expectedRel, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(found);
            Assert.True(File.Exists(found));
            Assert.Equal("ok", File.ReadAllText(found));
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { }
        }
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