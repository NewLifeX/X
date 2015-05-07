using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace NewLife.Cube.Precompiled
{
    /// <summary>页面助手类</summary>
    public static class Pages
    {
        /// <summary>路径是否存在</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public static bool IsExistByVirtualPath(string virtualPath)
        {
            if (virtualPath.StartsWith("~/"))
                virtualPath = virtualPath.Substring(1); var assembly = Assembly.LoadFrom(HttpContext.Current.Server.MapPath("~/bin") + "\\Falafel.Resources.dll");
            string result = string.Empty;
            virtualPath = "Falafel.Resources" + virtualPath.Replace('/', '.'); if (virtualPath.EndsWith("/"))
            {
                result = assembly.GetManifestResourceNames().First();
            }
            else
            {
                result = assembly.GetManifestResourceNames().FirstOrDefault(i => i.ToLower() == virtualPath.ToLower());
            } return string.IsNullOrEmpty(result) ? false : true;
        }

        /// <summary>根据路径获取页面模版</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public static string GetByVirtualPath(string virtualPath)
        {
            if (virtualPath.StartsWith("~/"))
                virtualPath = virtualPath.Substring(1);

            var assembly = Assembly.LoadFrom(HttpContext.Current.Server.MapPath("~/bin") + "\\Falafel.Resources.dll");
            virtualPath = "Falafel.Resources" + virtualPath.Replace('/', '.');
            virtualPath = assembly.GetManifestResourceNames().FirstOrDefault(i => i.ToLower() == virtualPath.ToLower());

            using (Stream stream = assembly.GetManifestResourceStream(virtualPath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    return result;
                }
            }
        }
    }
}