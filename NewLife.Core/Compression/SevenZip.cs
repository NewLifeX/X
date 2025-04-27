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

        // 附近文件
        if (p.IsNullOrEmpty())
        {
            var f = "7z.exe".GetFullPath();
            if (!File.Exists(f)) p = "7z/7z.exe".GetFullPath();
            if (!File.Exists(f)) p = "../7z/7z.exe".GetFullPath();
            if (!File.Exists(f)) p = "";
        }

        // 自动下载
        if (p.IsNullOrEmpty())
        {
            XTrace.WriteLine("准备下载7z扩展包");

            var set = Setting.Current;
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
    #endregion

    #region 压缩/解压缩        
    /// <summary>压缩文件</summary>
    /// <param name="path"></param>
    /// <param name="destFile"></param>
    /// <returns></returns>
    public Boolean Compress(String path, String destFile)
    {
        if (Directory.Exists(path)) path = path.GetFullPath().EnsureEnd("\\") + "*";

        return Run($"a \"{destFile}\" \"{path}\" -mx9 -ssw");
    }

    /// <summary>解压缩文件</summary>
    /// <param name="file"></param>
    /// <param name="destDir"></param>
    /// <param name="overwrite">是否覆盖目标同名文件</param>
    /// <returns></returns>
    public Boolean Extract(String file, String destDir, Boolean overwrite = false)
    {
        destDir.EnsureDirectory(false);

        var args = $"x \"{file}\" -o\"{destDir}\" -y -r";
        if (overwrite)
            args += " -aoa";
        else
            args += " -aos";

        return Run(args);
    }

    private Boolean Run(String args) => _7z.Run(args, 5000) == 0;
    #endregion
}