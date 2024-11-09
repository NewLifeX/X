using System;
using System.IO;
using Xunit;

namespace XUnitTest.IO;

[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class PathHelperTests
{
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
}