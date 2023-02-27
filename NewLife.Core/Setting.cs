using System.ComponentModel;
using NewLife.Configuration;
using NewLife.Log;

//[assembly: InternalsVisibleTo("XUnitTest.Core")]

namespace NewLife;

/// <summary>核心设置</summary>
[DisplayName("核心设置")]
[Config("Core")]
public class Setting : Config<Setting>
{
    #region 属性
    /// <summary>是否启用全局调试。默认启用</summary>
    [Description("全局调试。XTrace.Debug")]
    public Boolean Debug { get; set; } = true;

    /// <summary>日志等级，只输出大于等于该级别的日志，All/Debug/Info/Warn/Error/Fatal，默认Info</summary>
    [Description("日志等级。只输出大于等于该级别的日志，All/Debug/Info/Warn/Error/Fatal，默认Info")]
    public LogLevel LogLevel { get; set; } = LogLevel.Info;

    /// <summary>文件日志目录。默认Log子目录</summary>
    [Description("文件日志目录。默认Log子目录")]
    public String LogPath { get; set; } = "";

    /// <summary>日志文件上限。超过上限后拆分新日志文件，默认10MB，0表示不限制大小</summary>
    [Description("日志文件上限。超过上限后拆分新日志文件，默认10MB，0表示不限制大小")]
    public Int32 LogFileMaxBytes { get; set; } = 10;

    /// <summary>日志文件备份。超过备份数后，最旧的文件将被删除，默认100，0表示不限制个数</summary>
    [Description("日志文件备份。超过备份数后，最旧的文件将被删除，默认100，0表示不限制个数")]
    public Int32 LogFileBackups { get; set; } = 100;

    /// <summary>日志文件格式。默认{0:yyyy_MM_dd}.log，支持日志等级如 {1}_{0:yyyy_MM_dd}.log</summary>
    [Description("日志文件格式。默认{0:yyyy_MM_dd}.log，支持日志等级如 {1}_{0:yyyy_MM_dd}.log")]
    public String LogFileFormat { get; set; } = "{0:yyyy_MM_dd}.log";

    /// <summary>网络日志。本地子网日志广播udp://255.255.255.255:514，或者http://xxx:80/log</summary>
    [Description("网络日志。本地子网日志广播udp://255.255.255.255:514，或者http://xxx:80/log")]
    public String NetworkLog { get; set; } = "";

    /// <summary>数据目录。本地数据库目录，默认Data子目录</summary>
    [Description("数据目录。本地数据库目录，默认Data子目录")]
    public String DataPath { get; set; } = "";

    /// <summary>备份目录。备份数据库时存放的目录，默认Backup子目录</summary>
    [Description("备份目录。备份数据库时存放的目录，默认Backup子目录")]
    public String BackupPath { get; set; } = "";

    /// <summary>插件目录</summary>
    [Description("插件目录")]
    public String PluginPath { get; set; } = "Plugins";

    /// <summary>插件服务器。将从该网页上根据关键字分析链接并下载插件</summary>
    [Description("插件服务器。将从该网页上根据关键字分析链接并下载插件")]
    public String PluginServer { get; set; } = "http://x.newlifex.com/";

    /// <summary>辅助解析程序集。程序集加载过程中，被依赖程序集未能解析时，是否协助解析，默认false</summary>
    [Description("辅助解析程序集。程序集加载过程中，被依赖程序集未能解析时，是否协助解析，默认false")]
    public Boolean AssemblyResolve { get; set; }
    #endregion

    #region 方法
    /// <summary>加载完成后</summary>
    protected override void OnLoaded()
    {
        if (LogPath.IsNullOrEmpty()) LogPath = "Log";
        if (DataPath.IsNullOrEmpty()) DataPath = "Data";
        if (BackupPath.IsNullOrEmpty()) BackupPath = "Backup";
        if (LogFileFormat.IsNullOrEmpty()) LogFileFormat = "{0:yyyy_MM_dd}.log";

        if (PluginServer.IsNullOrWhiteSpace()) PluginServer = "http://x.newlifex.com/";

        base.OnLoaded();
    }

    /// <summary>获取插件目录</summary>
    /// <returns></returns>
    public String GetPluginPath() => PluginPath.GetBasePath();
    #endregion
}