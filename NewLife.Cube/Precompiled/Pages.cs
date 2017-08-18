using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NewLife.Cube.Precompiled
{
    /// <summary>页面助手类</summary>
    public static class Pages
    {
        /// <summary>路径是否存在</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public static Boolean IsExistByVirtualPath(String virtualPath)
        {
            if (virtualPath.StartsWith("~/")) virtualPath = virtualPath.Substring(1);

            var assembly = Assembly.LoadFrom(HttpContext.Current.Server.MapPath("~/bin") + "\\Falafel.Resources.dll");
            virtualPath = "Falafel.Resources" + virtualPath.Replace('/', '.');

            var result = String.Empty;
            if (virtualPath.EndsWith("/"))
                result = assembly.GetManifestResourceNames().First();
            else
                result = assembly.GetManifestResourceNames().FirstOrDefault(i => i.ToLower() == virtualPath.ToLower());

            return !result.IsNullOrEmpty();
        }

        /// <summary>根据路径获取页面模版</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public static String GetByVirtualPath(String virtualPath)
        {
            if (virtualPath.StartsWith("~/")) virtualPath = virtualPath.Substring(1);

            var assembly = Assembly.LoadFrom(HttpContext.Current.Server.MapPath("~/bin") + "\\Falafel.Resources.dll");
            virtualPath = "Falafel.Resources" + virtualPath.Replace('/', '.');
            virtualPath = assembly.GetManifestResourceNames().FirstOrDefault(i => i.ToLower() == virtualPath.ToLower());

            using (var stream = assembly.GetManifestResourceStream(virtualPath))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}