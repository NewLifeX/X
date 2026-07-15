using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

/// <summary>插件辅助测试</summary>
public class PluginHelperTests
{
    [Fact(DisplayName = "CreateClient工厂默认不为空")]
    public void CreateClientDefault()
    {
        var factory = PluginHelper.CreateClient;
        Assert.NotNull(factory);

        var client = factory("test");
        Assert.NotNull(client);
    }

    [Fact(DisplayName = "CreateClient可外部覆盖")]
    public void CreateClientCanBeOverridden()
    {
        var original = PluginHelper.CreateClient;
        try
        {
            var invoked = false;
            PluginHelper.CreateClient = name =>
            {
                invoked = true;
                Assert.Equal("myplugin", name);
                return null!;
            };

            var client = PluginHelper.CreateClient("myplugin");
            Assert.True(invoked);
        }
        finally
        {
            PluginHelper.CreateClient = original;
        }
    }

    [Fact(DisplayName = "LoadPlugin空typeName抛异常")]
    public void LoadPluginNullTypeNameThrows()
    {
        Assert.Throws<ArgumentNullException>(() => PluginHelper.LoadPlugin(null!, null, null, null));
        Assert.Throws<ArgumentNullException>(() => PluginHelper.LoadPlugin("", null, null, null));
    }

    [Fact(DisplayName = "LoadPlugin空dll返回null")]
    public void LoadPluginNoDllReturnsNull()
    {
        var type = PluginHelper.LoadPlugin("Some.Type", "显示名", null, null);
        Assert.Null(type);
    }

    [Fact(DisplayName = "LoadPlugin已加载类型直接返回")]
    public void LoadPluginAlreadyLoadedType()
    {
        var type = PluginHelper.LoadPlugin("System.String", null, null, null);
        Assert.NotNull(type);
        Assert.Equal(typeof(String), type);
    }

    [Fact(DisplayName = "LoadPlugin找不到DLL返回null")]
    public void LoadPluginNonExistentDllReturnsNull()
    {
        var type = PluginHelper.LoadPlugin("Some.Type", "测试", "nonexistent.dll", null);
        Assert.Null(type);
    }
}
