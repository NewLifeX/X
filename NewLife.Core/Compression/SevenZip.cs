using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using NewLife.Log;
using NewLife.Web;

namespace NewLife.Compression
{
    /// <summary>7Zip</summary>
    public class SevenZip
    {
        #region  基础
        private static String _7z = null;

        static SevenZip()
        {
            var p = "";

            #region 附近文件
            if (p.IsNullOrEmpty())
            {
                var f = "../7z/7z.exe".GetFullPath();
                if (File.Exists(f)) p = f;

                f = "7z/7z.exe".GetFullPath();
                if (File.Exists(f)) p = f;
            }
            #endregion

            #region 注册表
            //if (p.IsNullOrEmpty())
            //{
            //    var reg = Registry.LocalMachine.OpenSubKey("Software\\7-Zip");
            //    if (reg == null) reg = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\7-Zip");
            //    if (reg != null)
            //    {
            //        var d = reg.GetValue("Path") + "";
            //        var f = d.CombinePath("7z.exe");
            //        if (File.Exists(f)) p = f;
            //    }
            //}
            #endregion

            #region X组件缓存
            var cache = Path.GetPathRoot(".".GetFullPath()).CombinePath(@"\X\7z").GetFullPath();
            if (p.IsNullOrEmpty())
            {
                var f = cache.CombinePath("7z.exe");
                if (File.Exists(f)) p = f;
            }
            #endregion

            #region 自动下载
            if (p.IsNullOrEmpty())
            {
                XTrace.WriteLine("准备下载7z扩展包");

                var url = Setting.Current.PluginServer;
                var client = new WebClientX(true, true)
                {
                    Log = XTrace.Log
                };
                var dir = cache;
                var file = client.DownloadLinkAndExtract(url, "7z", dir);
                if (Directory.Exists(dir))
                {
                    var f = dir.CombinePath("7z.exe");
                    if (File.Exists(f)) p = f;
                }
            }
            #endregion

            if (!p.IsNullOrEmpty()) _7z = p.GetFullPath();

            XTrace.WriteLine("7Z目录 {0}", _7z);
        }

        /// <summary>实例化</summary>
        public SevenZip()
        {
#if DEBUG
            Log = XTrace.Log;
#endif
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

            return Run("a \"{0}\" \"{1}\" -mx9 -ssw".F(destFile, path));
        }

        /// <summary>解压缩文件</summary>
        /// <param name="file"></param>
        /// <param name="destDir"></param>
        /// <param name="overwrite">是否覆盖目标同名文件</param>
        /// <returns></returns>
        public Boolean Extract(String file, String destDir, Boolean overwrite = false)
        {
            destDir.EnsureDirectory(false);

            var args = "x \"{0}\" -o\"{1}\" -y -r".F(file, destDir);
            if (overwrite)
                args += " -aoa";
            else
                args += " -aos";

            return Run(args);
        }

        private Boolean Run(String args)
        {
            WriteLog("{0} {1}", _7z, args);

            //var p = new Process();
            //p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized; // 隐藏窗口            
            //p.StartInfo.FileName = _7z;
            //p.StartInfo.CreateNoWindow = false;
            //p.StartInfo.Arguments = args;
            //p.Start();
            //p.WaitForExit();

            //var rs = 0;
            //if (p.HasExited)
            //{
            //    rs = p.ExitCode;
            //    p.Close();
            //    if (rs != 0 && rs != 1) return false;
            //}
            //return true;

            var rs = _7z.Run(args, 5000);
            return rs == 0;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}