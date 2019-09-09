using System;
using System.IO;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Web
{
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
        public static Type LoadPlugin(String typeName, String disname, String dll, String linkName, String urls = null)
        {
            var type = typeName.GetTypeEx(true);
            if (type != null) return type;

            if (dll.IsNullOrEmpty()) return null;

            lock (typeName)
            {
                var set = Setting.Current;
                var plug = set.GetPluginPath();

                var file = "";
                if (!dll.IsNullOrEmpty())
                {
                    // 先检查当前目录，再检查插件目录
                    file = dll.GetFullPath();
                    if (!File.Exists(file) && Runtime.IsWeb) file = "Bin".GetFullPath().CombinePath(dll);
                    if (!File.Exists(file)) file = plug.CombinePath(dll);
                }

                if (urls.IsNullOrEmpty()) urls = set.PluginServer;

                // 如果本地没有数据库，则从网络下载
                if (!File.Exists(file))
                {
                    XTrace.WriteLine("{0}不存在或平台版本不正确，准备联网获取 {1}", disname ?? dll, urls);

                    var client = new WebClientX()
                    {
                        Log = XTrace.Log
                    };
                    var dir = Path.GetDirectoryName(file);
                    var file2 = client.DownloadLinkAndExtract(urls, linkName, dir);
                    client.TryDispose();
                }
                if (!File.Exists(file))
                {
                    XTrace.WriteLine("未找到 {0} {1}", disname, dll);
                    return null;
                }

                //return Assembly.LoadFrom(file).GetType(typeName);
                return typeName.GetTypeEx(true);
            }
        }
    }
}