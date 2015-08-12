using System.Collections.Generic;
using System.Text;
using NewLife.IO;

namespace System.IO
{
    /// <summary>路径操作帮助</summary>
    public static class PathHelper
    {
        #region 属性
        private static String _BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>基础目录。GetFullPath依赖于此，默认为当前应用程序域基础目录</summary>
        public static String BaseDirectory { get { return _BaseDirectory; } set { _BaseDirectory = value; } }
        #endregion

        #region 路径操作辅助
        /// <summary>获取文件或目录的全路径，过滤相对目录</summary>
        /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
        /// <param name="path">文件或目录</param>
        /// <returns></returns>
        public static String GetFullPath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            // 处理路径分隔符，兼容Windows和Linux
            var sep = Path.DirectorySeparatorChar;
            var sep2 = sep == '/' ? '\\' : '/';
            path = path.Replace(sep2, sep);
            //if (!Path.IsPathRooted(path))
            //!!! 注意：不能直接依赖于Path.IsPathRooted判断，/和\开头的路径虽然是绝对路径，但是它们不是驱动器级别的绝对路径
            if (path[0] == sep || path[0] == sep2 || !Path.IsPathRooted(path))
            {
                path = path.TrimStart('~');
                path = path.TrimStart(sep);

                path = Path.Combine(BaseDirectory, path);
            }

            return Path.GetFullPath(path);
        }

        /// <summary>确保目录存在，若不存在则创建</summary>
        /// <remarks>
        /// 斜杠结尾的路径一定是目录，无视第二参数；
        /// 默认是文件，这样子只需要确保上一层目录存在即可，否则如果把文件当成了目录，目录的创建会导致文件无法创建。
        /// </remarks>
        /// <param name="path">文件路径或目录路径，斜杠结尾的路径一定是目录，无视第二参数</param>
        /// <param name="isfile">该路径是否是否文件路径。文件路径需要取目录部分</param>
        /// <returns></returns>
        public static String EnsureDirectory(this String path, Boolean isfile = true)
        {
            if (String.IsNullOrEmpty(path)) return path;

            path = path.GetFullPath();
            if (File.Exists(path) || Directory.Exists(path)) return path;

            var dir = path;
            // 斜杠结尾的路径一定是目录，无视第二参数
            if (dir[dir.Length - 1] == Path.DirectorySeparatorChar)
                dir = Path.GetDirectoryName(path);
            else if (isfile)
                dir = Path.GetDirectoryName(path);

            /*!!! 基础类库的用法应该有明确的用途，而不是通过某些小伎俩去让人猜测 !!!*/

            //// 如果有圆点说明可能是文件
            //var p1 = dir.LastIndexOf('.');
            //if (p1 >= 0)
            //{
            //    // 要么没有斜杠，要么圆点必须在最后一个斜杠后面
            //    var p2 = dir.LastIndexOf('\\');
            //    if (p2 < 0 || p2 < p1) dir = Path.GetDirectoryName(path);
            //}

            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            return path;
        }

        /// <summary>合并多段路径</summary>
        /// <param name="path"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static String CombinePath(this String path, params String[] ps)
        {
            if (ps == null || ps.Length < 1) return path;
            if (path == null) path = String.Empty;

            //return Path.Combine(path, path2);
            foreach (var item in ps)
            {
                if (!item.IsNullOrEmpty()) path = Path.Combine(path, item);
            }
            return path;
        }
        #endregion

        #region 文件扩展
        /// <summary>文件路径作为文件信息</summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static FileInfo AsFile(this String file) { return new FileInfo(file.GetFullPath()); }

        /// <summary>从文件中读取数据</summary>
        /// <param name="file"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Byte[] ReadBytes(this FileInfo file, Int32 offset = 0, Int32 count = 0)
        {
            using (var fs = file.OpenRead())
            {
                fs.Position = offset;

                if (count <= 0) count = (Int32)(fs.Length - offset);

                return fs.ReadBytes(count);
            }
        }

        /// <summary>把数据写入文件指定位置</summary>
        /// <param name="file"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static FileInfo WriteBytes(this FileInfo file, Byte[] data, Int32 offset = 0)
        {
            using (var fs = file.OpenWrite())
            {
                fs.Position = offset;

                fs.Write(data, offset, data.Length);
            }

            return file;
        }

        /// <summary>读取所有文本，自动检测编码</summary>
        /// <remarks>性能较File.ReadAllText略慢，可通过提前检测BOM编码来优化</remarks>
        /// <param name="file"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String ReadText(this FileInfo file, Encoding encoding = null)
        {
            using (var fs = file.OpenRead())
            {
                if (encoding == null) encoding = fs.Detect() ?? Encoding.Default;
                using (var reader = new StreamReader(fs, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>把文本写入文件，自动检测编码</summary>
        /// <param name="file"></param>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static FileInfo WriteText(this FileInfo file, String text, Encoding encoding = null)
        {
            using (var fs = file.OpenWrite())
            {
                if (encoding == null) encoding = fs.Detect() ?? Encoding.Default;
                using (var writer = new StreamWriter(fs, encoding))
                {
                    writer.Write(text);
                }
            }

            return file;
        }

        /// <summary>复制到目标文件，目标文件必须已存在，且源文件较新</summary>
        /// <param name="fi">源文件</param>
        /// <param name="destFileName">目标文件</param>
        /// <returns></returns>
        public static Boolean CopyToIfNewer(this FileInfo fi, String destFileName)
        {
            // 源文件必须存在
            if (fi == null || !fi.Exists) return false;

            var dest = destFileName.AsFile();
            // 目标文件必须存在且源文件较新
            if (dest.Exists && fi.LastWriteTime > dest.LastWriteTime)
            {
                fi.CopyTo(destFileName, true);
                return true;
            }

            return false;
        }
        #endregion

        #region 目录扩展
        /// <summary>路径作为目录信息</summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static DirectoryInfo AsDirectory(this String dir) { return new DirectoryInfo(dir.GetFullPath()); }

        /// <summary>获取目录内所有符合条件的文件，支持多文件扩展匹配</summary>
        /// <param name="di">目录</param>
        /// <param name="exts">文件扩展列表。比如*.exe;*.dll;*.config</param>
        /// <param name="allSub">是否包含所有子孙目录文件</param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> GetAllFiles(this DirectoryInfo di, String exts = null, Boolean allSub = false)
        {
            if (di == null || !di.Exists) yield break;

            if (String.IsNullOrEmpty(exts)) exts = "*";
            var opt = allSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var pattern in exts.Split(";", "|"))
            {
                foreach (var item in di.GetFiles(pattern, opt))
                {
                    yield return item;
                }
            }
        }

        //public static IEnumerable<String> GetAllFileNames(this DirectoryInfo di, String searchPattern = null, Boolean allSub = true)
        //{
        //    var root = di.FullName.GetFullPath().EnsureEnd("\\");
        //    if (String.IsNullOrEmpty(searchPattern)) searchPattern = "*";
        //    var opt = allSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        //    foreach (var pattern in searchPattern.Split(";", "|"))
        //    {
        //        foreach (var item in di.GetFiles(pattern, opt))
        //        {
        //            yield return item.FullName.TrimStart(root);
        //        }
        //    }
        //}

        ///// <summary>从指定目录更新本目录的文件</summary>
        ///// <param name="di"></param>
        ///// <param name="src"></param>
        ///// <param name="searchPattern"></param>
        ///// <param name="allSub"></param>
        ///// <returns></returns>
        //public static Int32 UpdateFrom(this DirectoryInfo di, String src, String searchPattern = null, Boolean allSub = true)
        //{
        //    if (di == null || String.IsNullOrEmpty(di.FullName)) return 0;
        //    if (!String.IsNullOrEmpty(src) || !Directory.Exists(src)) return 0;

        //    var root = di.FullName.GetFullPath().EnsureEnd("\\");
        //    if (String.IsNullOrEmpty(searchPattern)) searchPattern = "*";
        //    var opt = allSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        //    var count = 0;
        //    foreach (var pattern in searchPattern.Split(";"))
        //    {
        //        foreach (var item in Directory.GetFiles(root, pattern, opt))
        //        {
        //            var srcFile = src.CombinePath(item.TrimStart(root));

        //            try
        //            {
        //                File.Copy(srcFile, item, true);

        //                count++;
        //            }
        //            catch { }
        //        }
        //    }
        //    return count;
        //}

        /// <summary>对比源目录和目标目录，复制双方都存在且源目录较新的文件</summary>
        /// <param name="di">源目录</param>
        /// <param name="destDirName">目标目录</param>
        /// <param name="exts">文件扩展列表。比如*.exe;*.dll;*.config</param>
        /// <param name="allSub">是否包含所有子孙目录文件</param>
        /// <param name="callback">复制每一个文件之前的回调</param>
        /// <returns></returns>
        public static Int32 CopyToIfNewer(this DirectoryInfo di, String destDirName, String exts = null, Boolean allSub = false, Action<String> callback = null)
        {
            var dest = destDirName.AsDirectory();
            if (!dest.Exists) return 0;

            var count = 0;

            // 目标目录根，用于截断
            var root = dest.FullName.EnsureEnd(Path.DirectorySeparatorChar.ToString());
            foreach (var item in dest.GetAllFiles(exts, allSub))
            {
                var name = item.FullName.TrimStart(root);
                var fi = di.FullName.CombinePath(name).AsFile();
                //fi.CopyToIfNewer(item.FullName);
                if (fi.Exists && item.Exists && fi.LastWriteTime > item.LastWriteTime)
                {
                    if (callback != null) callback(name);
                    fi.CopyTo(item.FullName, true);
                    count++;
                }
            }

            return count;
        }
        #endregion
    }
}