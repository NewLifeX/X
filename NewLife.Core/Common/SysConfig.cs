using System.ComponentModel;
using System.Reflection;
using NewLife.Configuration;
using NewLife.Reflection;
using NewLife.Security;

namespace NewLife.Common;

/// <summary>系统设置。提供系统名称、版本等基本设置</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/sysconfig
/// </remarks>
[DisplayName("System settings")]
public class SysConfig : Config<SysConfig>
{
    #region 属性
    /// <summary>系统名称</summary>
    [DisplayName("System name")]
    [Description("English name used to identify the system; must not contain spaces.")]
    public String Name { get; set; } = "";

    /// <summary>系统版本</summary>
    [DisplayName("System version")]
    [Description("Assembly version number, used for UI display.")]
    public String Version { get; set; } = "";

    /// <summary>显示名称</summary>
    [DisplayName("System display name")]
    [Description("User-visible name.")]
    public String DisplayName { get; set; } = "";

    /// <summary>公司</summary>
    [DisplayName("Company")]
    [Description("Company name, from assembly information.")]
    public String Company { get; set; } = "";

    /// <summary>应用实例。单应用多实例部署时用于唯一标识实例节点</summary>
    [DisplayName("Application instance")]
    [Description("Uniquely identifies the instance node when a single application is deployed with multiple instances.")]
    public Int32 Instance { get; set; }

    /// <summary>开发者模式</summary>
    [DisplayName("Developer mode")]
    [Description("Enables development and debugging features (SQL collection, backup, development operations).")]
    public Boolean Develop { get; set; } = true;

    /// <summary>启用</summary>
    [DisplayName("Enabled")]
    [Description("Whether the system is enabled.")]
    public Boolean Enable { get; set; } = true;

    /// <summary>安装时间</summary>
    [DisplayName("Install time")]
    [Description("First installation time of the system, used as the Cookie encryption key.")]
    public DateTime InstallTime { get; set; } = DateTime.Now;
    #endregion

    #region 方法
    /// <summary>加载后触发</summary>
    protected override void OnLoaded()
    {
        if (IsNew)
        {
            var asmx = SysAssembly;

            Name = asmx?.Name ?? "NewLife.Cube";
            Version = asmx?.Version ?? "0.1";
            DisplayName = (asmx?.Title ?? asmx?.Name) ?? "魔方平台";
            Company = asmx?.Company ?? "新生命开发团队";
            //Address = "新生命开发团队";

            if (DisplayName.IsNullOrEmpty()) DisplayName = "系统设置";
        }

        // 强制设置
        var name = GetSysName();
        if (!name.IsNullOrEmpty()) Name = name;

        // 本地实例，取IPv4地址后两段
        if (Instance <= 0)
        {
            try
            {
                var ip = NetHelper.MyIP();
                if (ip != null)
                {
                    var buf = ip.GetAddressBytes();
                    Instance = (buf[2] << 8) | buf[3];
                }
                else
                {
                    Instance = Rand.Next(1, 1024);
                }
            }
            catch
            {
                // 异常时随机
                Instance = Rand.Next(1, 1024);
            }
        }

        base.OnLoaded();
    }

    /// <summary>获取系统名</summary>
    /// <returns></returns>
    public static String? GetSysName()
    {
        // 从命令参数或环境变量获取系统名称，强制覆盖SysConfig，方便星尘发布根据命令行控制系统名称
        var name = "";
        // 命令参数
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].EqualIgnoreCase("-Name") && i + 1 < args.Length)
            {
                name = args[i + 1];
                break;
            }
        }

        // 环境变量
        if (name.IsNullOrEmpty()) name = Runtime.GetEnvironmentVariable("Name");

        return name;
    }

    /// <summary>系统主程序集</summary>
    public static AssemblyX? SysAssembly
    {
        get
        {
            try
            {
                var asm = AssemblyX.Entry;
                if (asm != null) return asm;

                var sm = Assembly.GetCallingAssembly();
                if (sm != null) return AssemblyX.Create(sm);

                var list = AssemblyX.GetMyAssemblies();

                // 最后编译那一个
                list = list.OrderByDescending(e => e.Compile)
                    .ThenByDescending(e => e.Name.EndsWithIgnoreCase("Web"))
                    .ToList();

                return list.FirstOrDefault();

            }
            catch { return null; }
        }
    }
    #endregion
}