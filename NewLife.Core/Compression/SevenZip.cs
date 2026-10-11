using System.Text;
using NewLife.Log;
using NewLife.Web;

namespace NewLife.Compression;

/// <summary>7Zip</summary>
public class SevenZip
{
    #region  基础
    private static readonly String _7z = null!;

    static SevenZip()
    {
        var p = "";
        var set = Setting.Current;

        // 附近文件
        if (p.IsNullOrEmpty())
        {
            p = "7z.exe".GetFullPath();
            if (!File.Exists(p)) p = set.PluginPath.CombinePath("7z.exe").GetFullPath();
            if (!File.Exists(p)) p = "7z/7z.exe".GetFullPath();
            if (!File.Exists(p)) p = "../7z/7z.exe".GetFullPath();
            if (!File.Exists(p)) p = "";
        }

        // 系统已安装的7-Zip
        if (p.IsNullOrEmpty()) p = FindInstalled();

        // 自动下载
        if (p.IsNullOrEmpty())
        {
            XTrace.WriteLine("准备下载7z扩展包");

            var url = set.PluginServer;
            var client = new WebClientX()
            {
                Log = XTrace.Log
            };
            var dir = set.PluginPath;
            var file = client.DownloadLinkAndExtract(url, "7z", dir);
            if (Directory.Exists(dir))
            {
                var f = dir.CombinePath("7z.exe");
                if (File.Exists(f)) p = f;
            }
        }

        if (!p.IsNullOrEmpty()) _7z = p.GetFullPath();

        XTrace.WriteLine("7Z目录 {0}", _7z);
    }

    /// <summary>查找系统已安装的7-Zip</summary>
    /// <returns>7z.exe 全路径，未找到时返回空字符串</returns>
    private static String FindInstalled()
    {
        // 7z.exe 是Windows程序，其它平台只认前面几个相对目录
        if (!Runtime.Windows) return "";

        foreach (var folder in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
        {
            var root = Environment.GetFolderPath(folder);
            if (root.IsNullOrEmpty()) continue;

            var file = root.CombinePath("7-Zip", "7z.exe");
            if (File.Exists(file)) return file;
        }

        return "";
    }
    #endregion

    #region 压缩/解压缩        
    /// <summary>压缩文件</summary>
    /// <param name="path"></param>
    /// <param name="destFile"></param>
    /// <returns></returns>
    public void Compress(String path, String destFile)
    {
        if (Directory.Exists(path)) path = path.GetFullPath().EnsureEnd("\\") + "*";

        Run($"a \"{destFile}\" \"{path}\" -mx9 -ssw");
    }

    /// <summary>解压缩文件</summary>
    /// <param name="file"></param>
    /// <param name="destDir"></param>
    /// <param name="overwrite">是否覆盖目标同名文件</param>
    /// <returns></returns>
    public void Extract(String file, String destDir, Boolean overwrite = false)
    {
        destDir.EnsureDirectory(false);

        var args = $"x \"{file}\" -o\"{destDir}\" -y -r";
        if (overwrite)
            args += " -aoa";
        else
            args += " -aos";

        Run(args);
    }

    private Int32 Run(String args)
    {
        if (_7z.IsNullOrEmpty()) throw new InvalidOperationException("未找到7z.exe，无法执行压缩/解压缩操作");

        // 7z 的执行时间取决于数据体积，大目录/冷备份常达数分钟，不能使用固定的短超时：
        // 超时会把 7z 强杀在半途，留下被截断的归档，而旧实现忽略返回值，表现为"静默成功"。
        // 负值表示一直等到 7z 自身退出，与本类文档描述的同步语义一致
        var rs = _7z.Run(args, -1);

        // 退出码：0 成功，1 警告（通常仍有可用产物），2 及以上为失败
        if (rs >= 2) throw new InvalidOperationException($"7z 执行失败，退出码 {rs}");

        return rs;
    }
    #endregion
}