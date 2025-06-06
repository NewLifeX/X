﻿using System.IO.Compression;
using NewLife;
using NewLife.Compression;
#if NET7_0_OR_GREATER
using System.Formats.Tar;
#endif

namespace System.IO;

/// <summary>路径操作帮助</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/path_helper
/// 
/// GetBasePath 依赖BasePath，支持参数和环境变量设置，主要用于存放X组件自身配置和日志等目录。
/// GetFullPath 依赖BaseDirectory，默认为应用程序域基础目录，支持参数和环境变量设置，此时跟GetBasePath保持一致。
/// 
/// GetFullPath更多用于表示当前工作目录，不可以轻易修改为Environment.CurrentDirectory。
/// 在vs运行应用时，Environment.CurrentDirectory是源码文件所在目录，而不是可执行文件目录。
/// 在StarAgent运行应用时，BasePath和Environment.CurrentDirectory都被修改为工作目录。
/// </remarks>
public static class PathHelper
{
    #region 属性
    /// <summary>基础目录。GetBasePath依赖于此，默认为当前应用程序域基础目录。用于X组件内部各目录，专门为函数计算而定制</summary>
    /// <remarks>
    /// 为了适应函数计算，该路径将支持从命令行参数和环境变量读取
    /// </remarks>
    public static String? BasePath { get; set; }

    /// <summary>基准目录。GetFullPath依赖于此，默认为当前应用程序域基础目录。支持BasePath参数修改</summary>
    /// <remarks>
    /// 为了适应函数计算，该路径将支持从命令行参数和环境变量读取
    /// </remarks>
    public static String? BaseDirectory { get; set; }
    #endregion

    #region 静态构造
    static PathHelper()
    {
        var dir = "";
        // 命令参数
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].EqualIgnoreCase("-BasePath", "--BasePath") && i + 1 < args.Length)
            {
                dir = args[i + 1];
                break;
            }
        }

        // 环境变量
        if (dir.IsNullOrEmpty()) dir = NewLife.Runtime.GetEnvironmentVariable("BasePath");

        if (!dir.IsNullOrEmpty()) BaseDirectory = dir;

        // 最终取应用程序域。Linux下编译为单文件时，应用程序释放到临时目录，应用程序域基路径不对，当前目录也不一定正确，唯有进程路径正确
        if (dir.IsNullOrEmpty()) dir = AppDomain.CurrentDomain.BaseDirectory;
        if (dir.IsNullOrEmpty()) dir = Environment.CurrentDirectory;

        // Xamarin 在 Android 上无法使用应用所在目录写入各种文件，改用临时目录
        //if (dir.IsNullOrEmpty() || dir == "/")
        //{
        //    if (args != null && args.Length > 0) dir = Path.GetDirectoryName(args[0]);
        //}
        if (dir.IsNullOrEmpty() || dir == "/")
        {
            dir = Path.GetTempPath();
        }

        if (!dir.IsNullOrEmpty()) BasePath = GetPath(dir, 1);
    }
    #endregion

    #region 路径操作辅助
    private static String GetPath(String path, Int32 mode)
    {
        // 处理路径分隔符，兼容Windows和Linux
        var sep = Path.DirectorySeparatorChar;
        var sep2 = sep == '/' ? '\\' : '/';
        path = path.Replace(sep2, sep);

        var dir = mode switch
        {
            1 => BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory ?? BasePath,
            2 => BasePath,
            3 => Environment.CurrentDirectory,
            _ => "",
        };
        if (dir.IsNullOrEmpty()) return Path.GetFullPath(path);

        // 处理网络路径
        if (path.StartsWith(@"\\", StringComparison.Ordinal)) return Path.GetFullPath(path);

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
            if (path[0] == sep2 || !Path.IsPathRooted(path))
            {
                path = path.TrimStart(sep);
                path = Path.Combine(dir, path);
            }
        }

        return Path.GetFullPath(path);
    }

    /// <summary>获取文件或目录基于应用程序域基目录的全路径，过滤相对目录</summary>
    /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
    /// <param name="path">文件或目录</param>
    /// <returns></returns>
    public static String GetFullPath(this String path)
    {
        if (String.IsNullOrEmpty(path)) return path;

        return GetPath(path, 1);
    }

    /// <summary>获取文件或目录的全路径，过滤相对目录。用于X组件内部各目录，专门为函数计算而定制</summary>
    /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
    /// <param name="path">文件或目录</param>
    /// <returns></returns>
    public static String GetBasePath(this String path)
    {
        if (String.IsNullOrEmpty(path)) return path;

        return GetPath(path, 2);
    }

    /// <summary>获取文件或目录基于当前目录的全路径，过滤相对目录</summary>
    /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
    /// <param name="path">文件或目录</param>
    /// <returns></returns>
    public static String GetCurrentPath(this String path)
    {
        if (String.IsNullOrEmpty(path)) return path;

        return GetPath(path, 3);
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
        if (dir[^1] == Path.DirectorySeparatorChar)
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
    public static String CombinePath(this String? path, params String[] ps)
    {
        path ??= String.Empty;
        if (ps == null || ps.Length <= 0) return path;

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
    public static FileInfo AsFile(this String file) => new(file.GetFullPath());

    /// <summary>从文件中读取数据</summary>
    /// <param name="file"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static Byte[] ReadBytes(this FileInfo file, Int32 offset = 0, Int32 count = -1)
    {
        using var fs = file.OpenRead();
        fs.Position = offset;

        if (count < 0) count = (Int32)(fs.Length - offset);

        var buf = new Byte[count];
        fs.ReadExactly(buf, 0, buf.Length);
        return buf;
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

    ///// <summary>读取所有文本，自动检测编码</summary>
    ///// <remarks>性能较File.ReadAllText略慢，可通过提前检测BOM编码来优化</remarks>
    ///// <param name="file"></param>
    ///// <param name="encoding"></param>
    ///// <returns></returns>
    //public static String ReadText(this FileInfo file, Encoding encoding = null)
    //{
    //    using var fs = file.OpenRead();
    //    if (encoding == null) encoding = fs.Detect() ?? Encoding.UTF8;
    //    using var reader = new StreamReader(fs, encoding);
    //    return reader.ReadToEnd();
    //}

    ///// <summary>把文本写入文件，自动检测编码</summary>
    ///// <param name="file"></param>
    ///// <param name="text"></param>
    ///// <param name="encoding"></param>
    ///// <returns></returns>
    //public static FileInfo WriteText(this FileInfo file, String text, Encoding encoding = null)
    //{
    //    using var fs = file.OpenWrite();
    //    if (encoding == null) encoding = fs.Detect() ?? Encoding.UTF8;
    //    using var writer = new StreamWriter(fs, encoding);
    //    writer.Write(text);

    //    return file;
    //}

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

    /// <summary>打开并读取</summary>
    /// <param name="file">文件信息</param>
    /// <param name="compressed">是否压缩</param>
    /// <param name="func">要对文件流操作的委托</param>
    /// <returns></returns>
    public static Int64 OpenRead(this FileInfo file, Boolean compressed, Action<Stream> func)
    {
        if (compressed)
        {
            using var fs = file.OpenRead();
            using var gs = new GZipStream(fs, CompressionMode.Decompress, true);
            using var bs = new BufferedStream(gs);
            func(bs);
            return fs.Position;
        }
        else
        {
            using var fs = file.OpenRead();
            func(fs);
            return fs.Position;
        }
    }

    /// <summary>打开并写入</summary>
    /// <param name="file">文件信息</param>
    /// <param name="compressed">是否压缩</param>
    /// <param name="func">要对文件流操作的委托</param>
    /// <returns></returns>
    public static Int64 OpenWrite(this FileInfo file, Boolean compressed, Action<Stream> func)
    {
        file.FullName.EnsureDirectory(true);

        using var fs = file.OpenWrite();
        if (compressed)
        {
            using var gs = new GZipStream(fs, CompressionLevel.Optimal, true);
            func(gs);
        }
        else
        {
            func(fs);
        }

        fs.SetLength(fs.Position);
        fs.Flush();

        return fs.Position;
    }

    /// <summary>解压缩</summary>
    /// <param name="fi"></param>
    /// <param name="destDir"></param>
    /// <param name="overwrite">是否覆盖目标同名文件</param>
    public static void Extract(this FileInfo fi, String destDir, Boolean overwrite = false)
    {
        if (destDir.IsNullOrEmpty()) destDir = Path.GetDirectoryName(fi.FullName).CombinePath(fi.Name);

        destDir = destDir.GetFullPath();
        //ZipFile.ExtractToDirectory(fi.FullName, destDir);

        if (fi.Name.EndsWithIgnoreCase(".zip"))
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            ZipFile.ExtractToDirectory(fi.FullName, destDir, overwrite);
#else
            using var zip = ZipFile.Open(fi.FullName, ZipArchiveMode.Read, null);
            var di = Directory.CreateDirectory(destDir);
            var fullName = di.FullName;
            foreach (var item in zip.Entries)
            {
                var fullPath = Path.GetFullPath(Path.Combine(fullName, item.FullName));
                if (!fullPath.StartsWith(fullName, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("IO_ExtractingResultsInOutside");

                if (Path.GetFileName(fullPath).Length == 0)
                {
                    if (item.Length != 0L) throw new IOException("IO_DirectoryNameWithData");

                    Directory.CreateDirectory(fullPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                    try
                    {
                        item.ExtractToFile(fullPath, overwrite);
                    }
                    catch { }
                }
            }
#endif
        }
        else if (fi.Name.EndsWithIgnoreCase(".tar", ".tar.gz", ".tgz"))
        {
#if NET7_0_OR_GREATER
            destDir.EnsureDirectory(false);
            if (fi.Name.EndsWithIgnoreCase(".tar"))
                System.Formats.Tar.TarFile.ExtractToDirectory(fi.FullName, destDir, overwrite);
            else
            {
                using var fs = fi.OpenRead();
                using var gs = new GZipStream(fs, CompressionMode.Decompress, true);
                using var bs = new BufferedStream(gs);
                System.Formats.Tar.TarFile.ExtractToDirectory(bs, destDir, overwrite);
            }
#else
            TarFile.ExtractToDirectory(fi.FullName, destDir, overwrite);
#endif
        }
        else
        {
            if (NewLife.Runtime.Windows)
                new SevenZip().Extract(fi.FullName, destDir);
            else
                throw new NotSupportedException();
        }
    }

    /// <summary>压缩文件</summary>
    /// <param name="fi"></param>
    /// <param name="destFile"></param>
    public static void Compress(this FileInfo fi, String destFile)
    {
        if (destFile.IsNullOrEmpty()) destFile = fi.Name + ".zip";

        destFile = destFile.GetFullPath();
        if (File.Exists(destFile)) File.Delete(destFile);

        if (destFile.EndsWithIgnoreCase(".zip"))
        {
            using var zip = ZipFile.Open(destFile, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(fi.FullName, fi.Name, CompressionLevel.Optimal);
        }
        else if (destFile.EndsWithIgnoreCase(".tar", ".tar.gz", ".tgz"))
        {
#if NET7_0_OR_GREATER
            if (destFile.EndsWithIgnoreCase(".tar"))
            {
                using var fs = new FileStream(destFile, FileMode.OpenOrCreate, FileAccess.Write);
                using var tarWriter = new TarWriter(fs, TarEntryFormat.Pax, false);
                tarWriter.WriteEntry(fi.FullName, fi.Name);
                fs.SetLength(fs.Position);
            }
            else
            {
                using var fs = new FileStream(destFile, FileMode.OpenOrCreate, FileAccess.Write);
                using var gs = new GZipStream(fs, CompressionMode.Compress, true);
                using var tarWriter = new TarWriter(gs, TarEntryFormat.Pax, false);
                tarWriter.WriteEntry(fi.FullName, fi.Name);

                gs.Flush();
                fs.SetLength(fs.Position);
            }
#else
            TarFile.CreateFromDirectory(fi.FullName, destFile);
#endif
        }
        else
        {
            if (NewLife.Runtime.Windows)
                new SevenZip().Compress(fi.FullName, destFile);
            else
                throw new NotSupportedException();
        }
    }
    #endregion

    #region 目录扩展
    /// <summary>路径作为目录信息</summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static DirectoryInfo AsDirectory(this String dir) => new(dir.GetFullPath());

    /// <summary>获取目录内所有符合条件的文件，支持多文件扩展匹配</summary>
    /// <param name="di">目录</param>
    /// <param name="exts">文件扩展列表。比如*.exe;*.dll;*.config</param>
    /// <param name="allSub">是否包含所有子孙目录文件</param>
    /// <returns></returns>
    public static IEnumerable<FileInfo> GetAllFiles(this DirectoryInfo di, String? exts = null, Boolean allSub = false)
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
    public static String[] CopyTo(this DirectoryInfo di, String destDirName, String? exts = null, Boolean allSub = false, Action<String>? callback = null)
    {
        if (!di.Exists) return [];

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
    public static String[] CopyToIfNewer(this DirectoryInfo di, String destDirName, String? exts = null, Boolean allSub = false, Action<String>? callback = null)
    {
        var dest = destDirName.AsDirectory();
        if (!dest.Exists) return [];

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
    public static String[] CopyIfNewer(this DirectoryInfo di, String[] source, String? exts = null, Boolean allSub = false)
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

    /// <summary>压缩</summary>
    /// <param name="di"></param>
    /// <param name="destFile"></param>
    public static void Compress(this DirectoryInfo di, String? destFile = null) => Compress(di, destFile, false);

    /// <summary>压缩</summary>
    /// <param name="di"></param>
    /// <param name="destFile"></param>
    /// <param name="includeBaseDirectory"></param>
    public static void Compress(this DirectoryInfo di, String? destFile, Boolean includeBaseDirectory)
    {
        if (destFile.IsNullOrEmpty()) destFile = di.Name + ".zip";

        if (File.Exists(destFile)) File.Delete(destFile);

        if (destFile.EndsWithIgnoreCase(".zip"))
            ZipFile.CreateFromDirectory(di.FullName, destFile, CompressionLevel.Optimal, includeBaseDirectory);
        else if (destFile.EndsWithIgnoreCase(".tar", ".tar.gz", ".tgz"))
        {
#if NET7_0_OR_GREATER
            if (destFile.EndsWithIgnoreCase(".tar"))
                System.Formats.Tar.TarFile.CreateFromDirectory(di.FullName, destFile, includeBaseDirectory);
            else
            {
                using var fs = new FileStream(destFile, FileMode.OpenOrCreate, FileAccess.Write);
                using var gs = new GZipStream(fs, CompressionMode.Compress, true);
                System.Formats.Tar.TarFile.CreateFromDirectory(di.FullName, gs, includeBaseDirectory);
                gs.Flush();
                fs.SetLength(fs.Position);
            }
#else
            TarFile.CreateFromDirectory(di.FullName, destFile);
#endif
        }
        else
        {
            if (NewLife.Runtime.Windows)
                new SevenZip().Compress(di.FullName, destFile);
            else
                throw new NotSupportedException();
        }
    }
    #endregion
}