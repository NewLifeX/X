using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;
using NewLife.Compression;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;

namespace NewLife.Net
{
    /// <summary>升级</summary>
    public class Upgrade
    {
        #region 属性
        private String _Server;
        /// <summary>服务器地址</summary>
        public String Server
        {
            get
            {
                if (_Server == null) _Server = Config.GetConfig<String>("NewLife.UpgradeServer", "http://up.newlifex.com");
                return _Server;
            }
            set { _Server = value; }
        }

        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private Version _Version;
        /// <summary>版本</summary>
        public Version Version { get { return _Version; } set { _Version = value; } }

        private DateTime _Compile;
        /// <summary>本地编译时间</summary>
        public DateTime Compile { get { return _Compile; } set { _Compile = value; } }

        private Boolean _AutoStart = true;
        /// <summary>更新完成以后自动启动主程序</summary>
        public Boolean AutoStart { get { return _AutoStart; } set { _AutoStart = value; } }

        private String _UpdatePath = "Update";
        /// <summary>更新目录</summary>
        public String UpdatePath { get { return _UpdatePath; } set { _UpdatePath = value; } }

        private String _TempPath = XTrace.TempPath;
        /// <summary>临时目录</summary>
        public String TempPath { get { return _TempPath; } set { _TempPath = value; } }
        #endregion

        #region 远程目标文件信息
        private Dictionary<String, String> _Files = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        /// <summary>文件列表</summary>
        public Dictionary<String, String> Files { get { return _Files; } set { _Files = value; } }

        private String _FileName;
        /// <summary>标题</summary>
        public String FileName { get { return _FileName; } set { _FileName = value; } }

        private String _FileUrl;
        /// <summary>下载地址</summary>
        public String FileUrl { get { return _FileUrl; } set { _FileUrl = value; } }

        private DateTime _FileTime;
        /// <summary>文件时间</summary>
        public DateTime FileTime { get { return _FileTime; } set { _FileTime = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个升级对象实例，获取当前应用信息</summary>
        public Upgrade()
        {
            var asm = Assembly.GetEntryAssembly();
            var asmx = AssemblyX.Create(asm);

            Version = asm.GetName().Version;
            Name = asm.GetName().Name;
            Compile = asmx.Compile;
        }
        #endregion

        #region 方法
        /// <summary>获取版本信息，检查是否需要更新</summary>
        /// <returns></returns>
        public Boolean Check()
        {
            var url = Server;
            // 如果配置地址未指定参数，则附加参数
            if (!url.Contains("{0}"))
            {
                if (!url.Contains("?"))
                    url += "?";
                else
                    url += "&";

                url += String.Format("Name={0}&Version={1}", Name, Version);
            }
            else
            {
                url = String.Format(url, Name, Version);
            }

            WriteLog("准备获取更新信息 {0}", url);

            var web = CreateClient();
            var html = web.GetHtml(url);
            if (String.IsNullOrEmpty(html)) return false;

            // 分析所有链接
            var reg = new Regex("<a(?:[^>]*) href=?\"([^>\"]*)?\"(?:[^>]*)>(?<内容>[^<]*)</a>", RegexOptions.IgnoreCase);
            var dic = Files;
            var buri = new Uri(url);
            foreach (Match item in reg.Matches(html))
            {
                var name = item.Groups[2].Value.Trim();
                url = item.Groups[1].Value.Trim();

                // 不是满足条件的name不要
                if (!name.StartsWithIgnoreCase(Name) && !name.Contains(Name)) continue;

                // 完善下载地址
                var uri = new Uri(buri, url);
                url = uri.ToString();

                WriteLog("发现 {0} {1}", name, url);

                // 分割名称，计算时间
                var p = name.LastIndexOf("_");
                if (p > 0)
                {
                    var ts = name.Substring(p + 1);
                    if (ts.StartsWith("20") && ts.Length >= 4 + 2 + 2 + 2 + 2 + 2)
                    {
                        var dt = new DateTime(
                            ts.Substring(0, 4).ToInt(),
                            ts.Substring(4, 2).ToInt(),
                            ts.Substring(6, 2).ToInt(),
                            ts.Substring(8, 2).ToInt(),
                            ts.Substring(10, 2).ToInt(),
                            ts.Substring(12, 2).ToInt());

                        if (dt.Year < DateTime.Now.Year + 10 && dt > FileTime)
                        {
                            FileTime = dt;
                            FileName = name;
                            FileUrl = url;
                        }
                    }
                }

                dic[name] = url;
            }

            return FileTime > DateTime.MinValue;
        }

        /// <summary>开始更新</summary>
        public void Download()
        {
            if (FileName == null) throw new Exception("没有可用新版本！");
            if (String.IsNullOrEmpty(FileUrl)) throw new Exception("升级包地址无效！");

            // 如果更新包不存在，则下载
            var file = UpdatePath.CombinePath(FileName).GetFullPath();
            if (!File.Exists(file))
            {
                WriteLog("准备下载 {0} 到 {1}", FileUrl, file);

                var sw = new Stopwatch();
                sw.Start();

                var web = CreateClient();
                web.DownloadFile(FileUrl, file.EnsureDirectory());

                sw.Stop();
                WriteLog("下载完成！大小{0:n0}字节，耗时{1:n0}ms", file.AsFile().Length, sw.ElapsedMilliseconds);
            }

            // 设置更新标记
            file += ".update";
            WriteLog("设置更新标记 {0}", file);
            File.CreateText(file).Close();
        }

        /// <summary>检查并执行更新操作</summary>
        public Boolean Update()
        {
            // 查找更新目录
            var fis = Directory.GetFiles(UpdatePath, "*.update");
            if (fis == null || fis.Length == 0) return false;

            var file = fis[0].GetFullPath().TrimEnd(".update");
            WriteLog("发现更新包 {0}，删除所有更新标记文件", file);
            foreach (var item in fis)
            {
                try
                {
                    File.Delete(item);
                }
                catch { }
            }

            if (!File.Exists(file)) return false;
            // 如果已经更新过，则也不再更新
            if (File.Exists(file + ".updated")) return false;

            // 解压更新程序包
            if (!file.EndsWithIgnoreCase(".zip")) return false;

            var p = TempPath.CombinePath(Path.GetFileNameWithoutExtension(file));
            WriteLog("解压缩更新包到临时目录 {0}", p);
            ZipFile.Extract(file, p);

            var updatebat = UpdatePath.CombinePath("update.bat").GetFullPath();
            MakeBat(updatebat, p, ".".GetFullPath());

            // 执行更新程序包
            var si = new ProcessStartInfo();
            si.FileName = updatebat;
            si.WorkingDirectory = Path.GetDirectoryName(si.FileName);
            if (!XTrace.Debug)
            {
                si.CreateNoWindow = true;
                si.WindowStyle = ProcessWindowStyle.Hidden;
            }
            si.UseShellExecute = false;
            Process.Start(si);

            WriteLog("已启动更新程序来升级，升级脚本 {0}", updatebat);

            // 设置更新标记
            file += ".updated";
            WriteLog("设置已更新标记 {0}", file);
            File.CreateText(file).Close();

            Application.Exit();

            return true;
        }
        #endregion

        #region 辅助
        private WebClientX _Client;
        private WebClientX CreateClient()
        {
            if (_Client != null) return _Client;

            var web = new WebClientX(true, true);
            web.UserAgent = "NewLife.Upgrade";
            return _Client = web;
        }

        void MakeBat(String updatebat, String tmpdir, String curdir)
        {
            var sb = new StringBuilder();

            // 等待一定时间后，干掉当前进程
            sb.AppendFormat("{0}\\ping -n 2 127.0.0.1 >nul", Environment.SystemDirectory);
            sb.AppendLine();
            sb.AppendFormat("{0}\\taskkill /F /PID {1}", Environment.SystemDirectory, Process.GetCurrentProcess().Id);
            sb.AppendLine();

            // 备份配置文件
            var cfgs = Directory.GetFiles(curdir);
            foreach (var item in cfgs)
            {
                if (item.EndsWithIgnoreCase(".config", ".xml"))
                {
                    sb.AppendFormat("move /Y \"{0}\" \"{0}.bak\"", item.GetFullPath());
                    sb.AppendLine();
                }
            }

            // 复制
            sb.AppendFormat("copy \"{0}\\*.*\" \"{1}\" /y", tmpdir, curdir);
            sb.AppendLine();
            sb.AppendLine("rd \"" + tmpdir + "\" /s /q");

            // 还原配置文件
            foreach (var item in cfgs)
            {
                if (item.EndsWithIgnoreCase(".config", ".xml"))
                {
                    sb.AppendFormat("move /Y \"{0}.bak\" \"{0}\"", item.GetFullPath());
                    sb.AppendLine();
                }
            }

            // 启动
            if (AutoStart)
            {
                var bin = Application.ExecutablePath;
                sb.AppendFormat("start /D \"{0}\" /I {1}", Path.GetDirectoryName(bin), bin);
                sb.AppendLine();
            }

#if !DEBUG
            sb.AppendFormat("del \"{0}\" /f/q", updatebat);
            sb.AppendLine();
#endif

            sb.AppendFormat("{0}\\ping -n 3 127.0.0.1 >nul", Environment.SystemDirectory);
            sb.AppendLine();
            sb.AppendLine("exit");

            File.WriteAllText(updatebat, sb.ToString(), Encoding.Default);
        }
        #endregion

        #region 日志
#if DEBUG
        private ILog _Log = XTrace.Log;
#else
        private ILog _Log = Logger.Null;
#endif
        /// <summary>日志对象</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            format = String.Format("[{0}]{1}", Name, format);
            if (Log != null) Log.Info(format, args);
        }
        #endregion
    }
}