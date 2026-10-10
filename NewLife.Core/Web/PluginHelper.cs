using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NewLife.Log;

namespace NewLife.Web;

/// <summary>插件助手</summary>
public static class PluginHelper
{
    /// <summary>创建客户端。便于外部自定义</summary>
    public static Func<String, WebClientX> CreateClient { get; set; } = linkName => new WebClientX { AuthKey = "NewLife", Log = XTrace.Log };

    /// <summary>加载插件</summary>
    /// <param name="typeName">插件类型</param>
    /// <param name="disname">显示名</param>
    /// <param name="dll">DLL文件</param>
    /// <param name="linkName">链接名。要在下载页面中搜索的链接名</param>
    /// <param name="urls">提供下载地址的多个目标页面</param>
    /// <returns></returns>
    [RequiresDynamicCode("插件动态加载依赖运行时加载程序集，NativeAOT 下不可用")]
    [RequiresUnreferencedCode("插件类型经反射加载，裁剪下类型成员可能缺失")]
    [UnconditionalSuppressMessage("AOT", "IL3000", Justification = "插件目录扫描依赖程序集物理位置；单文件/AOT 下 Location 为空即视为无插件，属设计性不支持（见分析稿 §5）")]
    public static Type? LoadPlugin(String typeName, String? disname, String? dll, String? linkName, String? urls = null)
    {
        if (typeName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(typeName));

        // NativeAOT 等环境明确报错，避免下载/加载后类型不可用
        DynamicCodeHelper.ThrowIfNotSupported("插件动态加载");

        //var type = typeName.GetTypeEx(true);
        var type = Type.GetType(typeName);
        if (type != null) return type;

        if (dll.IsNullOrEmpty()) return null;

        var set = Setting.Current;

        var file = "";
        if (!dll.IsNullOrEmpty())
        {
            // 先检查程序集所在目录，再检查当前目录、基准目录和插件目录。在应用发布时，插件很可能跟常规应用程序集放在同一目录下
            var exe = Assembly.GetEntryAssembly()?.Location;
            if (exe.IsNullOrEmpty()) exe = Assembly.GetCallingAssembly()?.Location;
            if (exe.IsNullOrEmpty()) exe = Assembly.GetExecutingAssembly()?.Location;
            if (!exe.IsNullOrEmpty()) file = Path.GetDirectoryName(exe).CombinePath(dll).GetFullPath();
            if (!File.Exists(file)) file = dll.GetCurrentPath();
            if (!File.Exists(file)) file = dll.GetFullPath();
            if (!File.Exists(file)) file = dll.GetBasePath();
            if (!File.Exists(file)) file = set.PluginPath.CombinePath(dll).GetFullPath();
            if (!File.Exists(file)) file = set.PluginPath.CombinePath(dll).GetBasePath();
        }

        // 尝试直接加载DLL
        if (File.Exists(file))
        {
            try
            {
                var asm = Assembly.LoadFrom(file);
                type = asm.GetType(typeName);
                if (type != null) return type;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        if (linkName.IsNullOrEmpty()) return null;

        // 按类型名锁定，超时取不到锁，则放弃
        if (!Monitor.TryEnter(typeName, 15_000)) return null;

        try
        {
            type = Type.GetType(typeName);
            if (type != null) return type;

            if (urls.IsNullOrEmpty()) urls = set.PluginServer;

            // 如果本地没有数据库，则从网络下载
            if (!File.Exists(file))
            {
                XTrace.WriteLine("{0}不存在或平台版本不正确，准备联网获取 {1}", !disname.IsNullOrEmpty() ? disname : dll, urls);

                //var client = new WebClientX()
                //{
                //    Log = XTrace.Log
                //};
                using var client = CreateClient(linkName);
                var dir = Path.GetDirectoryName(file);
                var file2 = client.DownloadLinkAndExtract(urls, linkName, dir!, true);
                //client.TryDispose();
            }
            if (!File.Exists(file))
            {
                XTrace.WriteLine("未找到 {0} {1}", disname, dll);
                return null;
            }

            //return Assembly.LoadFrom(file).GetType(typeName);

            type = Type.GetType(typeName);
            if (type != null) return type;

            // 尝试直接加载DLL
            if (File.Exists(file))
            {
                try
                {
                    var asm = Assembly.LoadFrom(file);
                    type = asm.GetType(typeName);
                    if (type != null) return type;
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }

            return null;
        }
        finally
        {
            Monitor.Exit(typeName);
        }
    }
}