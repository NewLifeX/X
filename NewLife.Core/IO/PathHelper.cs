using System.Collections.Generic;
using System.Text;
using NewLife.IO;
#if !__MOBILE__
using System.IO.Compression;
using NewLife.Compression;
#endif

namespace System.IO
{
    /// <summary>路径操作帮助</summary>
    public static class PathHelper
    {
        #region 属性
        /// <summary>基础目录。GetFullPath依赖于此，默认为当前应用程序域基础目录</summary>
#if __CORE__
        public static String BaseDirectory { get; set; } = AppContext.BaseDirectory;
#else
        public static String BaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
#endif
        #endregion

        #region 路径操作辅助
        private static String GetPath(String path, Int32 mode)
        {
            // 处理路径分隔符，兼容Windows和Linux
            var sep = Path.DirectorySeparatorChar;
            var sep2 = sep == '/' ? '\\' : '/';
            path = path.Replace(sep2, sep);

            var dir = "";
            switch (mode)
            {
                case 1:
                    dir = BaseDirectory;
                    break;
                case 2:
#if __CORE__
                    dir = AppContext.BaseDirectory;
#else
                    dir = AppDomain.CurrentDomain.BaseDirectory;
#endif
                    break;
#if !__CORE__
                case 3:
                    dir = Environment.CurrentDirectory;
                    break;
#endif
                default:
                    break;
            }
            if (dir.IsNullOrEmpty()) return Path.GetFullPath(path);

            // 处理网络路径
            if (path.StartsWith(@"\\")) return Path.GetFullPath(path);

            // 考虑兼容Linux
            if (!NewLife.Runtime.Mono)
            {
                //if (!Path.IsPathRooted(path))
                //!!! 注意：不能直接依赖于Path.IsPathRooted判断，/和\开头的路径虽然是绝对路径，但是它们不是驱动器级别的绝对路径
                if (/*path[0] == sep ||*/ path[0] == sep2 || !Path.IsPathRooted(path))
                {
                    path = path.TrimStart('~');

                    path = path.TrimStart(sep);
                    path = Path.Combine(dir, path);
                }
            }
            else
            {
                if (!path.StartsWith(dir))
                {
                    // path目录存在，不用再次拼接
                    if (!Directory.Exists(path))
                    {
                        path = path.TrimStart(sep);
                        path = Path.Combine(dir, path);
                    }
                }
            }

            return Path.GetFullPath(path);
        }

        /// <summary>获取文件或目录的全路径，过滤相对目录</summary>
        /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
        /// <param name="path">文件或目录</param>
        /// <returns></returns>
        public static String GetFullPath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            return GetPath(path, 1);
        }

        /// <summary>获取文件或目录基于应用程序域基目录的全路径，过滤相对目录</summary>
        /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
        /// <param name="path">文件或目录</param>
        /// <returns></returns>
        public static String GetBasePath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            return GetPath(path, 2);
        }

#if !__CORE__
        /// <summary>获取文件或目录基于当前目录的全路径，过滤相对目录</summary>
        /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
        /// <param name="path">文件或目录</param>
        /// <returns></returns>
        public static String GetCurrentPath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            return GetPath(path, 3);
        }
#endif

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
        public static Byte[] ReadBytes(this FileInfo file, Int32 offset = 0, Int32 count = -1)
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
                if (encoding == null) encoding = fs.Detect() ?? Encoding.UTF8;
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
                if (encoding == null) encoding = fs.Detect() ?? Encoding.UTF8;
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
#if !__MOBILE__ && !NET4
        /// <summary>解压缩</summary>
        /// <param name="fi"></param>
        /// <param name="destDir"></param>
        /// <param name="overwrite">是否覆盖目标同名文件</param>
        public static void Extract(this FileInfo fi, String destDir, Boolean overwrite = false)
        {
            if (destDir.IsNullOrEmpty()) destDir = fi.Name.GetFullPath();

            //ZipFile.ExtractToDirectory(fi.FullName, destDir);

            if (fi.Name.EndsWithIgnoreCase(".zip"))
            {
                using (var zip = ZipFile.Open(fi.FullName, ZipArchiveMode.Read, null))
                {
                    var di = Directory.CreateDirectory(destDir);
                    var fullName = di.FullName;
                    foreach (var current in zip.Entries)
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(fullName, current.FullName));
                        if (!fullPath.StartsWith(fullName, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new IOException("IO_ExtractingResultsInOutside");
                        }
                        if (Path.GetFileName(fullPath).Length == 0)
                        {
                            if (current.Length != 0L)
                            {
                                throw new IOException("IO_DirectoryNameWithData");
                            }
                            Directory.CreateDirectory(fullPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            try
                            {
                                current.ExtractToFile(fullPath, overwrite);
                            }
                            catch { }
                        }
                    }
                }
            }
            else
            {
                new SevenZip().Extract(fi.FullName, destDir);
            }
        }

        /// <summary>压缩文件</summary>
        /// <param name="fi"></param>
        /// <param name="destFile"></param>
        public static void Compress(this FileInfo fi, String destFile)
        {
            if (destFile.IsNullOrEmpty()) destFile = fi.Name + ".zip";

            if (File.Exists(destFile)) File.Delete(destFile);

            if (destFile.EndsWithIgnoreCase(".zip"))
            {
                using (var zf = ZipFile.Open(destFile, ZipArchiveMode.Create))
                {
                    zf.CreateEntryFromFile(fi.FullName, fi.Name, CompressionLevel.Optimal);
                }
            }
            else
            {
                new SevenZip().Compress(fi.FullName, destFile);
            }
        }
#elif !__MOBILE__
        /// <summary>解压缩</summary>
        /// <param name="fi"></param>
        /// <param name="destDir"></param>
        /// <param name="overwrite">是否覆盖目标同名文件</param>
        public static void Extract(this FileInfo fi, String destDir, Boolean overwrite = false)
        {
            if (destDir.IsNullOrEmpty()) destDir = fi.Name.GetFullPath();

            //ZipFile.ExtractToDirectory(fi.FullName, destDir);

            if (fi.Name.EndsWithIgnoreCase(".zip"))
            {
                ZipFile.ExtractToDirectory(fi.FullName, destDir, overwrite, false);
            }
            else
            {
                new SevenZip().Extract(fi.FullName, destDir);
            }
        }

        /// <summary>压缩文件</summary>
        /// <param name="fi"></param>
        /// <param name="destFile"></param>
        public static void Compress(this FileInfo fi, String destFile)
        {
            if (destFile.IsNullOrEmpty()) destFile = fi.Name + ".zip";

            if (File.Exists(destFile)) File.Delete(destFile);

            if (destFile.EndsWithIgnoreCase(".zip"))
            {
                ZipFile.CompressFile(fi.FullName, destFile);
            }
            else
            {
                new SevenZip().Compress(fi.FullName, destFile);
            }
        }
#endif
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

            foreach (var pattern in exts.Split(";", "|", ","))
            {
                foreach (var item in di.GetFiles(pattern, opt))
                {
                    yield return item;
                }
            }
        }

        /// <summary>复制目录中的文件</summary>
        /// <param name="di">源目录</param>
        /// <param name="destDirName">目标目录</param>
        /// <param name="exts">文件扩展列表。比如*.exe;*.dll;*.config</param>
        /// <param name="allSub">是否包含所有子孙目录文件</param>
        /// <param name="callback">复制每一个文件之前的回调</param>
        /// <returns></returns>
        public static String[] CopyTo(this DirectoryInfo di, String destDirName, String exts = null, Boolean allSub = false, Action<String> callback = null)
        {
            if (!di.Exists) return new String[0];

            var list = new List<String>();

            // 来源目录根，用于截断
            var root = di.FullName.EnsureEnd(Path.DirectorySeparatorChar.ToString());
            foreach (var item in di.GetAllFiles(exts, allSub))
            {
                var name = item.FullName.TrimStart(root);
                var dst = destDirName.CombinePath(name);
                callback?.Invoke(name);
                item.CopyTo(dst.EnsureDirectory(true), true);

                list.Add(dst);
            }

            return list.ToArray();
        }

        /// <summary>对比源目录和目标目录，复制双方都存在且源目录较新的文件</summary>
        /// <param name="di">源目录</param>
        /// <param name="destDirName">目标目录</param>
        /// <param name="exts">文件扩展列表。比如*.exe;*.dll;*.config</param>
        /// <param name="allSub">是否包含所有子孙目录文件</param>
        /// <param name="callback">复制每一个文件之前的回调</param>
        /// <returns></returns>
        public static String[] CopyToIfNewer(this DirectoryInfo di, String destDirName, String exts = null, Boolean allSub = false, Action<String> callback = null)
        {
            var dest = destDirName.AsDirectory();
            if (!dest.Exists) return new String[0];

            var list = new List<String>();

            // 目标目录根，用于截断
            var root = dest.FullName.EnsureEnd(Path.DirectorySeparatorChar.ToString());
            // 遍历目标目录，拷贝同名文件
            foreach (var item in dest.GetAllFiles(exts, allSub))
            {
                var name = item.FullName.TrimStart(root);
                var fi = di.FullName.CombinePath(name).AsFile();
                //fi.CopyToIfNewer(item.FullName);
                if (fi.Exists && item.Exists && fi.LastWriteTime > item.LastWriteTime)
                {
                    callback?.Invoke(name);
                    fi.CopyTo(item.FullName, true);
                    list.Add(fi.FullName);
                }
            }

            return list.ToArray();
        }

        /// <summary>从多个目标目录复制较新文件到当前目录</summary>
        /// <param name="di">当前目录</param>
        /// <param name="source">多个目标目录</param>
        /// <param name="exts">文件扩展列表。比如*.exe;*.dll;*.config</param>
        /// <param name="allSub">是否包含所有子孙目录文件</param>
        /// <returns></returns>
        public static String[] CopyIfNewer(this DirectoryInfo di, String[] source, String exts = null, Boolean allSub = false)
        {
            var list = new List<String>();
            var cur = di.FullName;
            foreach (var item in source)
            {
                // 跳过当前目录
                if (item.GetFullPath().EqualIgnoreCase(cur)) continue;

                Console.WriteLine("复制 {0} => {1}", item, cur);

                try
                {
                    var rs = item.AsDirectory().CopyToIfNewer(cur, exts, allSub, name =>
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\t{1}\t{0}", name, item.CombinePath(name).AsFile().LastWriteTime.ToFullString());
                        Console.ResetColor();
                    });
                    if (rs != null && rs.Length > 0) list.AddRange(rs);
                }
                catch (Exception ex) { Console.WriteLine(" " + ex.Message); }
            }

            return list.ToArray();
        }

#if !__MOBILE__
        /// <summary>压缩</summary>
        /// <param name="di"></param>
        /// <param name="destFile"></param>
        public static void Compress(this DirectoryInfo di, String destFile = null)
        {
            if (destFile.IsNullOrEmpty()) destFile = di.Name + ".zip";

            if (File.Exists(destFile)) File.Delete(destFile);

#if !NET4
            if (destFile.EndsWithIgnoreCase(".zip"))
                ZipFile.CreateFromDirectory(di.FullName, destFile, CompressionLevel.Optimal, true);
            else
                new SevenZip().Compress(di.FullName, destFile);
#else
            new SevenZip().Compress(di.FullName, destFile);
#endif
        }
#endif
        #endregion
    }
}