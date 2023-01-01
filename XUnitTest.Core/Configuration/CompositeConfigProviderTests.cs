using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class CompositeConfigProviderTests
{
    [Fact]
    public void Test1()
    {
        var cp1 = new HttpConfigProvider
        {
            Server = "http://star.newlifex.com:6600",
            //Server = "http://localhost:6600",
            AppId = "Test"
        };
        cp1.LoadAll();
        var cp2 = JsonConfigProvider.LoadAppSettings();
        cp2.LoadAll();

        var ks1 = cp1.Keys.ToList();
        var ks2 = cp2.Keys.ToList();

        var provider = new CompositeConfigProvider(cp1, cp2);
        var ks = provider.Keys.ToList();

        Assert.Equal(cp1, provider.Configs[0]);
        Assert.Equal(cp2, provider.Configs[1]);
        Assert.Equal("Composite", provider.Name);
        Assert.Equal(cp1.Root, provider.Root);
        Assert.Equal(cp1.IsNew, provider.IsNew);
        Assert.NotEqual(cp1.GetConfig, provider.GetConfig);

        //Assert.Equal(ks1.Count + ks2.Count, ks.Count);
        Assert.Equal("NewLife开发团队", provider["Title"]);
        Assert.Equal("https://newlifex.com/", provider["url"]);
    }

    [Fact]
    public void Test2()
    {
        var cp1 = new HttpConfigProvider
        {
            Server = "http://star.newlifex.com:6600",
            //Server = "http://localhost:6600",
            AppId = "Test"
        };
        //cp1.LoadAll();
        var cp2 = JsonConfigProvider.LoadAppSettings();
        //cp2.LoadAll();

        var provider = new CompositeConfigProvider(cp2, cp1);
        provider.LoadAll();

        var ks1 = cp1.Keys.ToList();
        var ks2 = cp2.Keys.ToList();
        var ks = provider.Keys.ToList();

        //Assert.Equal(ks1.Count + ks2.Count, ks.Count);
        Assert.Equal("本地标题", provider["Title"]);
        Assert.Equal("https://newlifex.com/", provider["url"]);
    }

    [Fact]
    public void Load()
    {
        var cp1 = new HttpConfigProvider
        {
            Server = "http://star.newlifex.com:6600",
            //Server = "http://localhost:6600",
            AppId = "Test"
        };
        var cp2 = JsonConfigProvider.LoadAppSettings();

        var provider = new CompositeConfigProvider(cp1, cp2);

        {
            var dic = provider.Load<Dictionary<String, Object>>();

            Assert.NotNull(dic);
            Assert.True(dic.Count >= 2);
        }

        {
            var dic = provider.Load<Dictionary<String, Object>>("ConnectionStrings");

            Assert.NotNull(dic);
            Assert.Equal(4, dic.Count);
        }
    }
}