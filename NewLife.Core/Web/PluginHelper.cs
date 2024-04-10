using System.Reflection;
using NewLife.Log;

namespace NewLife.Web;

/// <summary>插件助手</summary>
public static class PluginHelper
{
    /// <summary>加载插件</summary>
    /// <param name="typeName"></param>
    /// <param name="disname"></param>
    /// <param name="dll"></param>
    /// <param name="linkName"></param>
    /// <param name="urls">提供下载地址的多个目标页面</param>
    /// <returns></returns>
    public static Type? LoadPlugin(String typeName, String? disname, String dll, String linkName, String? urls = null)
    {
        if (typeName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(typeName));

        //var type = typeName.GetTypeEx(true);
        var type = Type.GetType(typeName);
        if (type != null) return type;

        if (dll.IsNullOrEmpty()) return null;

        var set = Setting.Current;

        var file = "";
        if (!dll.IsNullOrEmpty())
        {
            // 先检查当前目录，再检查插件目录
            file = dll.GetCurrentPath();
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

        //lock (typeName)
        {
            type = Type.GetType(typeName);
            if (type != null) return type;

            if (urls.IsNullOrEmpty()) urls = set.PluginServer;

            // 如果本地没有数据库，则从网络下载
            if (!File.Exists(file))
            {
                XTrace.WriteLine("{0}不存在或平台版本不正确，准备联网获取 {1}", !disname.IsNullOrEmpty() ? disname : dll, urls);

                var client = new WebClientX()
                {
                    Log = XTrace.Log
                };
                var dir = Path.GetDirectoryName(file);
                var file2 = client.DownloadLinkAndExtract(urls, linkName, dir!);
                client.TryDispose();
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
    }
}