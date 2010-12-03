using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Web;

namespace XCode.Common
{
    /// <summary>
    /// 文件资源
    /// </summary>
    public static class FileSource
    {
        /// <summary>
        /// 释放文件
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dest"></param>
        /// <param name="overWrite"></param>
        public static void ReleaseFile(String filename, String dest, Boolean overWrite)
        {
            if (String.IsNullOrEmpty(filename)) return;
            if (String.IsNullOrEmpty(dest)) dest = filename;

            if (dest.Length < 2 || dest[1] != Path.VolumeSeparatorChar)
            {
                String str = AppDomain.CurrentDomain.BaseDirectory;
                if (!String.IsNullOrEmpty(HttpRuntime.AppDomainAppId)) str = HttpRuntime.BinDirectory;
                dest = Path.Combine(str, dest);
            }

            if (File.Exists(dest) && !overWrite) return;

            String path = Path.GetDirectoryName(dest);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            try
            {
                Byte[] buffer = GetFileResource(filename);

                if (File.Exists(dest)) File.Delete(dest);

                File.WriteAllBytes(dest, buffer);
            }
            catch { }

        }

        /// <summary>
        /// 释放程序集文件
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Assembly GetAssembly(String filename)
        {
            String path = AppDomain.CurrentDomain.BaseDirectory;
            if (!String.IsNullOrEmpty(HttpRuntime.AppDomainAppId)) path = HttpRuntime.BinDirectory;
            path = Path.Combine(path, filename);

            if (!File.Exists(path))
            {
                Byte[] buffer = GetFileResource(filename);
                return Assembly.Load(buffer);
            }
            return null;
        }

        /// <summary>
        /// 获取文件字节码
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Byte[] GetFileResource(String filename)
        {
            if (String.IsNullOrEmpty(filename)) return null;

            String name = String.Empty;
            String[] ss = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            if (ss != null && ss.Length > 0)
            {
                //找到资源名
                foreach (String item in ss)
                {
                    if (String.Equals(item, filename, StringComparison.OrdinalIgnoreCase))
                    {
                        name = item;
                        break;
                    }
                }
                if (String.IsNullOrEmpty(name))
                {
                    foreach (String item in ss)
                    {
                        if (item.Contains(filename))
                        {
                            name = item;
                            break;
                        }
                    }
                }
                if (!String.IsNullOrEmpty(name))
                {
                    //读取资源，并写入到文件
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                    if (stream != null)
                    {
                        Byte[] buffer = new Byte[stream.Length];
                        Int32 count = stream.Read(buffer, 0, buffer.Length);
                        return buffer;
                    }
                }
            }
            return null;
        }
    }
}
