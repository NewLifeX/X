using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NewLife.IO
{
    /// <summary>文件资源</summary>
    public static class FileSource
    {
        /// <summary>释放文件</summary>
        /// <param name="asm"></param>
        /// <param name="fileName"></param>
        /// <param name="destFile"></param>
        /// <param name="overWrite"></param>
        public static void ReleaseFile(this Assembly asm, String fileName, String destFile = null, Boolean overWrite = false)
        {
            if (fileName.IsNullOrEmpty()) return;

            if (asm == null) asm = Assembly.GetCallingAssembly();
            var stream = GetFileResource(asm, fileName);
            if (stream == null) throw new ArgumentException("filename", String.Format("在程序集{0}中无法找到名为{1}的资源！", asm.GetName().Name, fileName));

            if (destFile.IsNullOrEmpty()) destFile = fileName;
            destFile = destFile.GetFullPath();

            if (File.Exists(destFile) && !overWrite) return;

            //var path = Path.GetDirectoryName(dest);
            //if (!path.IsNullOrWhiteSpace() && !Directory.Exists(path)) Directory.CreateDirectory(path);
            destFile.EnsureDirectory(true);
            try
            {
                if (File.Exists(destFile)) File.Delete(destFile);

                using (var fs = File.Create(destFile))
                {
                    IOHelper.CopyTo(stream, fs);
                }
            }
            catch { }
            finally { stream.Dispose(); }
        }

        /// <summary>释放文件夹</summary>
        /// <param name="asm"></param>
        /// <param name="prefix"></param>
        /// <param name="dest"></param>
        /// <param name="overWrite"></param>
        public static void ReleaseFolder(this Assembly asm, String prefix, String dest, Boolean overWrite = false)
        {
            if (asm == null) asm = Assembly.GetCallingAssembly();
            ReleaseFolder(asm, prefix, dest, overWrite, null);
        }

        /// <summary>释放文件夹</summary>
        /// <param name="asm"></param>
        /// <param name="prefix"></param>
        /// <param name="dest"></param>
        /// <param name="overWrite"></param>
        /// <param name="filenameResolver"></param>
        public static void ReleaseFolder(this Assembly asm, String prefix, String dest, Boolean overWrite = false, Func<String, String> filenameResolver = null)
        {
            if (asm == null) asm = Assembly.GetCallingAssembly();

            // 找到符合条件的资源
            var names = asm.GetManifestResourceNames();
            if (names == null || names.Length < 1) return;
            IEnumerable<String> ns = null;
            if (prefix.IsNullOrWhiteSpace())
                ns = names.AsEnumerable();
            else
                ns = names.Where(e => e.StartsWithIgnoreCase(prefix));

            if (String.IsNullOrEmpty(dest)) dest = AppDomain.CurrentDomain.BaseDirectory;
            dest = dest.GetFullPath();

            // 开始处理
            foreach (var item in ns)
            {
                var stream = asm.GetManifestResourceStream(item);

                // 计算filename
                String filename = null;
                // 去掉前缀
                if (filenameResolver != null) filename = filenameResolver(item);

                if (String.IsNullOrEmpty(filename))
                {
                    filename = item;
                    if (!String.IsNullOrEmpty(prefix)) filename = filename.Substring(prefix.Length);
                    if (filename[0] == '.') filename = filename.Substring(1);

                    var ext = Path.GetExtension(item);
                    filename = filename.Substring(0, filename.Length - ext.Length);
                    filename = filename.Replace(".", @"\") + ext;
                    filename = Path.Combine(dest, filename);
                }

                if (File.Exists(filename) && !overWrite) return;

                //var path = Path.GetDirectoryName(filename);
                //if (!path.IsNullOrWhiteSpace() && !Directory.Exists(path)) Directory.CreateDirectory(path);
                filename.EnsureDirectory(true);
                try
                {
                    if (File.Exists(filename)) File.Delete(filename);

                    using (var fs = File.Create(filename))
                    {
                        IOHelper.CopyTo(stream, fs);
                    }
                }
                catch { }
                finally { stream.Dispose(); }
            }
        }

        /// <summary>获取文件资源</summary>
        /// <param name="asm"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Stream GetFileResource(this Assembly asm, String filename)
        {
            if (String.IsNullOrEmpty(filename)) return null;

            var name = String.Empty;
            if (asm == null) asm = Assembly.GetCallingAssembly();
            var ss = asm.GetManifestResourceNames();
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