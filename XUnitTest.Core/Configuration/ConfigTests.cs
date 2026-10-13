using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class ConfigTests
{
    public class TestConfig : Config<TestConfig>
    {
        public String Name { get; set; } = "default";
        public Int32 Value { get; set; } = 42;
        public Boolean Enabled { get; set; } = true;
    }

    /// <summary>仅用于模拟“配置初始化进行中”的独立配置类型，避免影响其它用例</summary>
    public class InitHoldConfig : Config<InitHoldConfig>
    {
        public String? Tag { get; set; }
    }

    [Fact]
    public void Current_ReturnsInstance()
    {
        var cfg = TestConfig.Current;

        Assert.NotNull(cfg);
        Assert.Equal("default", cfg.Name);
        Assert.Equal(42, cfg.Value);
        Assert.True(cfg.Enabled);
    }

    [Fact]
    public void Current_IsSingleton()
    {
        var cfg1 = TestConfig.Current;
        var cfg2 = TestConfig.Current;

        Assert.Same(cfg1, cfg2);
    }

    [Fact]
    public void Current_ResetReloads()
    {
        var cfg1 = TestConfig.Current;
        cfg1.Name = "modified";

        // 通过反射置空 _Current 触发重新加载
        var field = typeof(Config<TestConfig>).GetField("_Current",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, null);

        var cfg2 = TestConfig.Current;
        Assert.NotNull(cfg2);
        // 重新加载后恢复默认值
        Assert.Equal("default", cfg2.Name);
    }

    [Fact]
    public void Provider_NotNull()
    {
        Assert.NotNull(Config<TestConfig>.Provider);
    }

    [Fact]
    public void Save_NoException()
    {
        var cfg = TestConfig.Current;
        var ex = Record.Exception(() => cfg.Save());

        Assert.Null(ex);
    }

    [Fact(DisplayName = "配置初始化中_TryGetCurrent不阻塞且返回空")]
    public async Task TryGetCurrent_ReturnsNullWhileInitializing()
    {
        // 模拟另一线程正在初始化 InitHoldConfig：占用其监视器（Config 的锁对象为 typeof(TConfig)）
        using var hold = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var holder = Task.Run(() =>
        {
            lock (typeof(InitHoldConfig))
            {
                hold.Set();
                release.Wait(10_000);
            }
        });
        Assert.True(hold.Wait(5_000), "占用线程未就绪");

        try
        {
            // 初始化进行中必须快速返回 null；若等待，将与“持配置锁等待类初始化/程序集解析”的场景互锁
            var cfg0 = await Task.Run(() => Config<InitHoldConfig>.TryGetCurrent()).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Null(cfg0);
        }
        finally
        {
            release.Set();
            await holder.WaitAsync(TimeSpan.FromSeconds(5));
        }

        // 只读已初始化的实例：未初始化时返回空，绝不触发初始化（程序集解析回调等路径依赖该契约）
        Assert.Null(Config<InitHoldConfig>.TryGetCurrent());

        // 需要初始化时走 Current
        var cfg = InitHoldConfig.Current;
        Assert.NotNull(cfg);
        Assert.Same(cfg, Config<InitHoldConfig>.TryGetCurrent());
    }
}
