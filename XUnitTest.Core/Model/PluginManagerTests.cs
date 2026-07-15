using NewLife.Model;
using Xunit;

namespace XUnitTest.Model;

/// <summary>测试 PluginManager 插件框架基本功能</summary>
public class PluginManagerTests
{
    [Fact(DisplayName = "创建 PluginManager 实例")]
    public void Create()
    {
        var pm = new PluginManager();
        Assert.NotNull(pm);
        Assert.Null(pm.Plugins);
    }

    [Fact(DisplayName = "Load 后空插件集合")]
    public void Load_Empty()
    {
        var pm = new PluginManager
        {
            Identity = "TestHost"
        };
        pm.Load();

        Assert.NotNull(pm.Plugins);
        // 测试项目中没有插件实现，因此应返回空数组
        Assert.Empty(pm.Plugins);
    }

    [Fact(DisplayName = "Load+Init 空插件不抛异常")]
    public void Load_Init_Empty()
    {
        var pm = new PluginManager
        {
            Identity = "TestHost"
        };
        pm.Load();
        // Init 空列表不应抛异常
        pm.Init();

        Assert.NotNull(pm.Plugins);
        Assert.Empty(pm.Plugins);
    }

    [Fact(DisplayName = "Dispose 释放不抛异常")]
    public void Dispose()
    {
        var pm = new PluginManager();
        pm.Load();
        pm.Init();

        // Dispose 应正常完成
        pm.Dispose();
        Assert.Null(pm.Plugins);
    }

    [Fact(DisplayName = "IServiceProvider 返回自身")]
    public void ServiceProvider_Self()
    {
        var pm = new PluginManager();
        var sp = (IServiceProvider)pm;

        var svc = sp.GetService(typeof(PluginManager));
        Assert.Same(pm, svc);

        // 未设置 Provider 时，其他类型应返回 null
        var other = sp.GetService(typeof(String));
        Assert.Null(other);
    }

    [Fact(DisplayName = "LoadPlugins 枚举当前程序集插件类型")]
    public void LoadPlugins()
    {
        var pm = new PluginManager
        {
            Identity = "TestHost"
        };

        var types = pm.LoadPlugins().ToList();
        // 测试项目中没有 IPlugin 实现，期望空列表
        Assert.NotNull(types);
    }
}
