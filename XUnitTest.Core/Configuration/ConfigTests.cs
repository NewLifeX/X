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
}
