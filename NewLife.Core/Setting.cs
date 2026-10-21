using System.ComponentModel;
using System.Runtime.CompilerServices;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Log;

[assembly: InternalsVisibleTo("XUnitTest.Core, PublicKey=00240000048000001401000006020000002400005253413100080000010001000d41eb3bdab5c2150958b46c95632b7e4dcb0af77ed8637bd8543875bc2443d01273143bb46655a48a92efa76251adc63ccca6d0e9cef2e0ce93e32b5043bea179a6c710981be4a71703a03e10960643f7df091f499cf60183ef0e4e4e2eebf26e25cea0eebf87c8a6d7f8130c283fc3f747cb90623f0aaa619825e3fcd82f267a0f4bfd26c9f2a6b5a62a6b180b4f6d1d091fce6bd60a9aa9aa5b815b833b44e0f2e58b28a354cb20f52f31bb3b3a7c54f515426537e41f9c20c07e51f9cab8abc311daac19a41bd473a51c7386f014edf1863901a5c29addc89da2f2659c9c1e95affd6997396b9680e317c493e974a813186da277ff9c1d1b30e33cb5a2f6")]
[assembly: InternalsVisibleTo("XUnitTest.Net6, PublicKey=00240000048000001401000006020000002400005253413100080000010001000d41eb3bdab5c2150958b46c95632b7e4dcb0af77ed8637bd8543875bc2443d01273143bb46655a48a92efa76251adc63ccca6d0e9cef2e0ce93e32b5043bea179a6c710981be4a71703a03e10960643f7df091f499cf60183ef0e4e4e2eebf26e25cea0eebf87c8a6d7f8130c283fc3f747cb90623f0aaa619825e3fcd82f267a0f4bfd26c9f2a6b5a62a6b180b4f6d1d091fce6bd60a9aa9aa5b815b833b44e0f2e58b28a354cb20f52f31bb3b3a7c54f515426537e41f9c20c07e51f9cab8abc311daac19a41bd473a51c7386f014edf1863901a5c29addc89da2f2659c9c1e95affd6997396b9680e317c493e974a813186da277ff9c1d1b30e33cb5a2f6")]

namespace NewLife;

/// <summary>核心设置</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/setting
/// </remarks>
[DisplayName("Core settings")]
[Config("Core")]
public class Setting : Config<Setting>
{
    #region 属性
    /// <summary>是否启用全局调试。默认启用</summary>
    [Description("Global debugging. Enables XTrace.Debug.")]
    public Boolean Debug { get; set; } = true;

    /// <summary>日志等级，只输出大于等于该级别的日志，All/Debug/Info/Warn/Error/Fatal，默认Info</summary>
    [Description("Log level. Only logs at or above this level are output: All/Debug/Info/Warn/Error/Fatal. Default Info.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Info;

    /// <summary>文件日志目录。默认Log子目录</summary>
    [Description("File log directory. Defaults to the Log subdirectory.")]
    public String LogPath { get; set; } = "";

    /// <summary>日志文件上限。超过上限后拆分新日志文件，默认10MB，0表示不限制大小</summary>
    [Description("Log file size limit (MB). Splits a new log file when exceeded. Default 10 MB; 0 means unlimited.")]
    public Int32 LogFileMaxBytes { get; set; } = 10;

    /// <summary>日志文件备份。超过备份数后，最旧的文件将被删除，网络安全法要求至少保存6个月日志，默认200，0表示不限制个数</summary>
    [Description("Log file backups. The oldest files are deleted when the count is exceeded. Cybersecurity law requires at least 6 months of logs. Default 200; 0 means unlimited.")]
    public Int32 LogFileBackups { get; set; } = 200;

    /// <summary>日志文件格式。默认{0:yyyy_MM_dd}.log，支持日志等级如 {1}_{0:yyyy_MM_dd}.log</summary>
    [Description("Log file format. Default {0:yyyy_MM_dd}.log; supports level placeholders such as {1}_{0:yyyy_MM_dd}.log.")]
    public String LogFileFormat { get; set; } = "{0:yyyy_MM_dd}.log";

    /// <summary>日志行格式。默认Time|ThreadId|Kind|Name|Message，还支持Level</summary>
    [Description("Log line format. Default Time|ThreadId|Kind|Name|Message; Level is also supported.")]
    public String LogLineFormat { get; set; } = "Time|ThreadId|Kind|Name|Message";

    /// <summary>网络日志。本地子网日志广播udp://255.255.255.255:514，或者http://xxx:80/log</summary>
    [Description("Network log. Broadcasts local subnet logs to udp://255.255.255.255:514, or http://xxx:80/log.")]
    public String NetworkLog { get; set; } = "";

    /// <summary>数据目录。本地数据库目录，默认Data子目录</summary>
    [Description("Data directory. Local database directory; defaults to the Data subdirectory.")]
    public String DataPath { get; set; } = "";

    /// <summary>备份目录。备份数据库时存放的目录，默认Backup子目录</summary>
    [Description("Backup directory. Directory for database backups; defaults to the Backup subdirectory.")]
    public String BackupPath { get; set; } = "";

    /// <summary>插件目录</summary>
    [Description("Plugin directory. Local plugin directory; defaults to the Plugins subdirectory.")]
    public String PluginPath { get; set; } = "";

    /// <summary>插件服务器。将从该网页上根据关键字分析链接并下载插件，部分嵌入式设备不支持https</summary>
    [Description("Plugin server. Analyzes links by keyword and downloads plugins from this page. Some embedded devices do not support HTTPS.")]
    public String PluginServer { get; set; } = "http://x.newlifex.com/";

    /// <summary>辅助解析程序集。程序集加载过程中，被依赖程序集未能解析时，是否协助解析，默认false</summary>
    [Description("Assembly resolve assistance. Whether to help resolve missing dependency assemblies during assembly loading. Default false.")]
    public Boolean AssemblyResolve { get; set; }

    /// <summary>服务地址。用户访问的外网地址，反向代理之外，用于内部构造其它Url（如SSO），或者向注册中心登记，多地址逗号隔开</summary>
    [Description("Service address. Public address visited by users, outside the reverse proxy, used to construct other URLs (such as SSO) or register with the registry. Separate multiple addresses with commas.")]
    public String ServiceAddress { get; set; } = "";

    /// <summary>界面语言。留空跟随系统；设置后固定界面与日志语言，不再跟随请求语言</summary>
    /// <remarks>
    /// 完整清单见 <see cref="Localization.SupportedCultures"/>，显示名称见 <see cref="Localization.GetLanguages"/>，
    /// 管理端下拉框可直接用 <see cref="Localization.GetLanguageOptions"/>。
    /// 裸语言码（en、ja、de-AT）由 <see cref="Localization.NormalizeCulture"/> 归一化；运行时注册的语言同样可用。
    /// </remarks>
    [Description("UI language. Values: zh-CN/zh-TW/en-US/es-ES/de-DE/ja-JP/fr-FR/pt-BR/ru-RU/ko-KR/ar-SA/bn-BD/cs-CZ/da-DK/el-GR/fa-IR/fi-FI/he-IL/hi-IN/hu-HU/id-ID/it-IT/ms-MY/nb-NO/nl-NL/pl-PL/ro-RO/sv-SE/th-TH/tr-TR/uk-UA/vi-VN. Empty follows the system; a supported language fixes the UI and log language regardless of request culture.")]
    public String Language { get; set; } = "";
    #endregion

    #region 方法
    /// <summary>加载完成后</summary>
    protected override void OnLoaded()
    {
        // 多应用项目，需要把基础目录向上提升一级
        var root = "../";
        var di = ".".AsDirectory();
        if (di.Name.StartsWithIgnoreCase("netcoreapp", "net2", "net4", "net5", "net6", "net7", "net8", "net9", "net10") && di.Parent != null)
        {
            root = "../../";
            di = di.Parent;
        }
        var name = SysConfig.GetSysName() ?? SysConfig.SysAssembly?.Name;
        if (name.IsNullOrEmpty()) name = di.Name;
        if (name.EndsWithIgnoreCase("Web", "Api", "Server", "Service", "Job"))
        {
            // 日志目录分开，其它目录共用
            if (LogPath.IsNullOrEmpty()) LogPath = $"{root}{di.Name}Log";
            if (DataPath.IsNullOrEmpty()) DataPath = $"{root}Data";
            if (BackupPath.IsNullOrEmpty()) BackupPath = $"{root}Backup";
            if (PluginPath.IsNullOrEmpty()) PluginPath = $"{root}Plugins";
        }
        else
        {
            if (LogPath.IsNullOrEmpty()) LogPath = "Log";
            if (DataPath.IsNullOrEmpty()) DataPath = "Data";
            if (BackupPath.IsNullOrEmpty()) BackupPath = "Backup";
            if (PluginPath.IsNullOrEmpty()) PluginPath = "Plugins";
        }
        if (LogFileFormat.IsNullOrEmpty()) LogFileFormat = "{0:yyyy_MM_dd}.log";

        if (PluginServer.IsNullOrWhiteSpace()) PluginServer = "http://x.newlifex.com/";

        base.OnLoaded();

        ApplyLanguage();
    }

    /// <summary>保存配置后立即应用语言设置</summary>
    public override void Save()
    {
        base.Save();

        ApplyLanguage();
    }

    /// <summary>应用语言设置</summary>
    /// <remarks>
    /// 留空表示自动识别，不覆盖宿主显式设置的 <see cref="Localization.Culture"/> 与 <see cref="Localization.LogCulture"/>；
    /// 语言不受支持时同样保持自动模式。
    /// </remarks>
    public void ApplyLanguage()
    {
        if (Language.IsNullOrWhiteSpace())
        {
            // 配置清空后撤销此前强制的语言，恢复自动识别；宿主自行设置的语言不受影响
            if (Localization.ForcedLanguage != null) Localization.SetLanguage(null);
            return;
        }

        // 配置的语言不受支持时保持自动模式，不覆盖宿主显式设置
        if (Localization.NormalizeCulture(Language) == null) return;

        Localization.SetLanguage(Language);
    }

    /// <summary>获取插件目录</summary>
    /// <returns></returns>
    public String GetPluginPath() => PluginPath.GetBasePath();
    #endregion
}