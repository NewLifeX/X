using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Linq;
using System.Reflection;
using System.Web;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>文件资源</summary>
    public static class FileSource
    {
        /// <summary>
        /// 释放文件
        /// </summary>
        /// <param name="asm"></param>
        /// <param name="filename"></param>
        /// <param name="dest"></param>
        /// <param name="overWrite"></param>
        public static void ReleaseFile(Assembly asm, String filename, String dest, Boolean overWrite)
        {
            if (asm == null || String.IsNullOrEmpty(filename)) return;

            //Stream stream = asm.GetManifestResourceStream(filename);
            Stream stream = GetFileResource(asm, filename);
            if (stream == null) throw new ArgumentException("filename", String.Format("在程序集{0}中无法找到名为{1}的资源！", asm.GetName().Name, filename));

            if (String.IsNullOrEmpty(dest)) dest = filename;

            if (!Path.IsPathRooted(dest))
            {
                String str = Runtime.IsWeb ? HttpRuntime.BinDirectory : AppDomain.CurrentDomain.BaseDirectory;
                dest = Path.Combine(str, dest);
            }

            if (File.Exists(dest) && !overWrite) return;

            String path = Path.GetDirectoryName(dest);
            if (!path.IsNullOrWhiteSpace() && !Directory.Exists(path)) Directory.CreateDirectory(path);
            try
            {
                if (File.Exists(dest)) File.Delete(dest);

                using (FileStream fs = File.Create(dest))
                {
                    IOHelper.CopyTo(stream, fs);
                }
            }
            catch { }
            finally { stream.Dispose(); }
        }

        /// <summary>
        /// 释放文件夹
        /// </summary>
        /// <param name="asm"></param>
        /// <param name="prefix"></param>
        /// <param name="dest"></param>
        /// <param name="overWrite"></param>
        public static void ReleaseFolder(Assembly asm, String prefix, String dest, Boolean overWrite)
        {
            ReleaseFolder(asm, prefix, dest, overWrite, null);
        }

        /// <summary>
        /// 释放文件夹
        /// </summary>
        /// <param name="asm"></param>
        /// <param name="prefix"></param>
        /// <param name="dest"></param>
        /// <param name="overWrite"></param>
        /// <param name="filenameResolver"></param>
        public static void ReleaseFolder(Assembly asm, String prefix, String dest, Boolean overWrite, Func<String, String> filenameResolver)
        {
            if (asm == null) return;

            // 找到符合条件的资源
            String[] names = asm.GetManifestResourceNames();
            if (names == null || names.Length < 1) return;
            IEnumerable<String> ns = null;
            if (prefix.IsNullOrWhiteSpace())
                ns = names.AsEnumerable();
            else
                ns = names.Where(e => e.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (String.IsNullOrEmpty(dest)) dest = AppDomain.CurrentDomain.BaseDirectory;

            if (!Path.IsPathRooted(dest))
            {
                String str = Runtime.IsWeb ? HttpRuntime.BinDirectory : AppDomain.CurrentDomain.BaseDirectory;
                dest = Path.Combine(str, dest);
            }

            // 开始处理
            foreach (String item in ns)
            {
                Stream stream = asm.GetManifestResourceStream(item);

                // 计算filename
                String filename = null;
                // 去掉前缀
                if (filenameResolver != null) filename = filenameResolver(item);

                if (String.IsNullOrEmpty(filename))
                {
                    filename = item;
                    if (!String.IsNullOrEmpty(prefix)) filename = filename.Substring(prefix.Length);
                    if (filename[0] == '.') filename = filename.Substring(1);

                    String ext = Path.GetExtension(item);
                    filename = filename.Substring(0, filename.Length - ext.Length);
                    filename = filename.Replace(".", @"\") + ext;
                    filename = Path.Combine(dest, filename);
                }

                if (File.Exists(filename) && !overWrite) return;

                String path = Path.GetDirectoryName(filename);
                if (!path.IsNullOrWhiteSpace() && !Directory.Exists(path)) Directory.CreateDirectory(path);
                try
                {
                    if (File.Exists(filename)) File.Delete(filename);

                    using (FileStream fs = File.Create(filename))
                    {
                        IOHelper.CopyTo(stream, fs);
                    }
                }
                catch { }
                finally { stream.Dispose(); }
            }
        }

        /// <summary>
        /// 获取文件资源
        /// </summary>
        /// <param name="asm"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Stream GetFileResource(Assembly asm, String filename)
        {
            if (asm == null || String.IsNullOrEmpty(filename)) return null;

            String name = String.Empty;
            String[] ss = asm.GetManifestResourceNames();
            if (ss != null && ss.Length > 0)
            {
                //找到资源名
                name = ss.FirstOrDefault(e => e == filename);
                if (String.IsNullOrEmpty(name)) name = ss.FirstOrDefault(e => e.EqualIgnoreCase(filename));
                if (String.IsNullOrEmpty(name)) name = ss.FirstOrDefault(e => e.EndsWith(filename));

                if (!String.IsNullOrEmpty(name)) return asm.GetManifestResourceStream(name);
            }
            return null;
        }
    }
}