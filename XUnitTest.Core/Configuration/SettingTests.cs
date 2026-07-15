using NewLife;
using Xunit;

namespace XUnitTest.Configuration;

/// <summary>核心配置测试</summary>
public class SettingTests
{
    [Fact(DisplayName = "Setting.Current不为空")]
    public void CurrentIsNotNull()
    {
        var setting = Setting.Current;
        Assert.NotNull(setting);
    }

    [Fact(DisplayName = "默认日志级别为Info")]
    public void DefaultLogLevel()
    {
        var setting = Setting.Current;
        Assert.NotNull(setting);

        // 默认日志级别应为 Info
        Assert.Equal(NewLife.Log.LogLevel.Info, setting.LogLevel);
    }

    [Fact(DisplayName = "默认调试开关为true")]
    public void DefaultDebug()
    {
        var setting = Setting.Current;
        Assert.NotNull(setting);
        Assert.True(setting.Debug);
    }

    [Fact(DisplayName = "属性读写")]
    public void PropertyReadWrite()
    {
        var setting = Setting.Current;
        Assert.NotNull(setting);

        // 备份原始值
        var original = setting.DataPath;

        // 设置临时值
        setting.DataPath = "/tmp/test";
        Assert.Equal("/tmp/test", setting.DataPath);

        // 恢复
        setting.DataPath = original;

        // 插件服务器地址应为默认值
        Assert.NotNull(setting.PluginServer);
    }

    [Fact(DisplayName = "日志文件格式默认值")]
    public void LogFileFormatDefault()
    {
        var setting = Setting.Current;
        Assert.NotNull(setting);

        Assert.Equal("{0:yyyy_MM_dd}.log", setting.LogFileFormat);
        Assert.Equal(10, setting.LogFileMaxBytes);
        Assert.Equal(200, setting.LogFileBackups);
    }
}
