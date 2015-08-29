using System;
using System.IO;
using System.Reflection;
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
        /// <param name="url"></param>
        /// <returns></returns>
        public static Type LoadPlugin(String typeName, String disname, String dll, String linkName, String url)
        {
            var type = typeName.GetTypeEx(true);
            if (type != null) return type;

            var file = dll;
            file = Setting.Current.GetPluginPath().CombinePath(file);
            file = file.EnsureDirectory();

            // 如果本地没有数据库，则从网络下载
            if (!dll.IsNullOrEmpty() || !File.Exists(file))
            {
                XTrace.WriteLine("{0}不存在或平台版本不正确，准备联网获取 {1}", disname ?? dll, url);

                var client = new WebClientX(true, true);
                client.Log = XTrace.Log;
                var dir = !dll.IsNullOrEmpty() ? Path.GetDirectoryName(file) : ".".GetFullPath();
                var file2 = client.DownloadLinkAndExtract(url, linkName, dir);
            }
            if (!dll.IsNullOrEmpty() && !File.Exists(file))
            {
                XTrace.WriteLine("未找到 {0} {1}", disname, dll);
                return null;
            }

            type = typeName.GetTypeEx(true);
            if (type != null) return type;

            //var assembly = Assembly.LoadFrom(file);
            //if (assembly == null) return null;

            //type = assembly.GetType(typeName);
            //if (type == null) type = AssemblyX.Create(assembly).GetType(typeName);
            return type;
        }
    }
}