using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Net
{
    /// <summary>升级更新</summary>
    /// <remarks>
    /// 优先比较版本Version，再比较时间Time。
    /// 自动更新的难点在于覆盖正在使用的exe/dll文件，通过改名可以解决。
    /// </remarks>
    public class Upgrade
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>服务器地址</summary>
        public String Server { get; set; }

        /// <summary>版本</summary>
        public Version Version { get; set; }

        /// <summary>本地编译时间</summary>
        public DateTime Time { get; set; }

        /// <summary>更新目录</summary>
        public String UpdatePath { get; set; } = "Update";

        /// <summary>目标目录</summary>
        public String DestinationPath { get; set; } = ".";

        /// <summary>超链接信息</summary>
        public Link Link { get; set; }

        /// <summary>更新源文件</summary>
        public String SourceFile { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个升级对象实例，获取当前应用信息</summary>
        public Upgrade()
        {
            var asm = Assembly.GetEntryAssembly();
            var asmx = AssemblyX.Create(asm);

            Version = asm.GetName().Version;
            Name = asm.GetName().Name;
            Time = asmx.Compile;

            Server = NewLife.Setting.Current.PluginServer;
        }
        #endregion

        #region 方法
        /// <summary>获取版本信息，检查是否需要更新</summary>
        /// <returns></returns>
        public Boolean Check()
        {
            // 删除备份文件
            DeleteBuckup(DestinationPath);

            var url = Server;

            WriteLog("检查资源包 {0}", url);

            var web = CreateClient();
            var html = web.GetHtml(url);
            var links = Link.Parse(html, url, item => item.Name.ToLower().Contains(Name.ToLower())); 
            if (links == null || links.Length == 0)
            {
                WriteLog("找不到资源包");
                return false;
            }

            // 先比较版本
            if (Version > new Version(0, 0))
            {
                var link = links.OrderByDescending(e => e.Version).FirstOrDefault();
                if (link.Version > Version)
                {
                    Link = link;
                    WriteLog("线上版本[{0}]较新 {1}>{2}", link.FullName, link.Version, Version);
                }
                else
                    WriteLog("线上版本[{0}]较旧 {1}<={2}", link.FullName, link.Version, Version);
            }
            // 再比较时间
            else
            {
                var link = links.OrderByDescending(e => e.Time).FirstOrDefault();
                // 只有文件时间大于编译时间才更新，需要考虑文件编译后过一段时间才打包
                if (link.Time > Time.AddMinutes(10))
                {
                    Link = link;
                    WriteLog("线上版本[{0}]较新 {1}>{2}", link.FullName, link.Time, Time);
                }
                else
                    WriteLog("线上版本[{0}]较旧 {1}<={2}", link.FullName, link.Time, Time);
            }

            return Link != null;
        }

        /// <summary>开始更新</summary>
        public void Download()
        {
            var link = Link;
            if (link == null) throw new Exception("没有可用新版本！");
            if (String.IsNullOrEmpty(link.Url)) throw new Exception("升级包地址无效！");

            // 如果更新包不存在，则下载
            var file = UpdatePath.CombinePath(link.FullName).GetBasePath();
            if (!File.Exists(file))
            {
                WriteLog("准备下载 {0} 到 {1}", link.Url, file);

                var sw = Stopwatch.StartNew();

                var web = CreateClient();
                TaskEx.Run(() => web.DownloadFileAsync(link.Url, file)).Wait();

                sw.Stop();
                WriteLog("下载完成！大小{0:n0}字节，耗时{1:n0}ms", file.AsFile().Length, sw.ElapsedMilliseconds);
            }

            SourceFile = file;
        }

        /// <summary>检查并执行更新操作</summary>
        public Boolean Update()
        {
            // 删除备份文件
            DeleteBuckup(DestinationPath);

            var file = SourceFile;

            if (!File.Exists(file)) return false;

            WriteLog("发现更新包 {0}", file);

            // 解压更新程序包
            if (!file.EndsWithIgnoreCase(".zip", ".7z")) return false;

            var tmp = XTrace.TempPath.CombinePath(Path.GetFileNameWithoutExtension(file)).GetBasePath();
            WriteLog("解压缩更新包到临时目录 {0}", tmp);
            file.AsFile().Extract(tmp, true);

            // 拷贝替换更新
            CopyAndReplace(tmp, DestinationPath);

            return true;
        }

        /// <summary>启动当前应用的新进程。当前进程退出</summary>
        public void Run()
        {
            // 启动进程
            var exe = Assembly.GetEntryAssembly().Location;
            WriteLog("启动进程 {0}", exe);
            Process.Start(exe);

            WriteLog("退出当前进程");
            if (!Runtime.IsConsole) Process.GetCurrentProcess().CloseMainWindow();
            Environment.Exit(0);
            Process.GetCurrentProcess().Kill();
        }
        #endregion

        #region 辅助
        private WebClientX _Client;
        private WebClientX CreateClient()
        {
            if (_Client != null) return _Client;

            var web = new WebClientX();
            return _Client = web;
        }

        /// <summary>删除备份文件</summary>
        /// <param name="dest">目标目录</param>
        public static void DeleteBuckup(String dest)
        {
            // 删除备份
            var di = dest.AsDirectory();
            var fs = di.GetAllFiles("*.del", true);
            foreach (var item in fs)
            {
                try
                {
                    item.Delete();
                }
                catch { }
            }
        }

        /// <summary>拷贝并替换。正在使用锁定的文件不可删除，但可以改名</summary>
        /// <param name="source">源目录</param>
        /// <param name="dest">目标目录</param>
        public static void CopyAndReplace(String source, String dest)
        {
            var di = source.AsDirectory();

            // 来源目录根，用于截断
            var root = di.FullName.EnsureEnd(Path.DirectorySeparatorChar.ToString());
            foreach (var item in di.GetAllFiles(null, true))
            {
                var name = item.FullName.TrimStart(root);
                var dst = dest.CombinePath(name).GetBasePath();

                // 如果是应用配置文件，不要更新
                if (dst.EndsWithIgnoreCase(".exe.config")) continue;

                // 如果是exe/dll，则先改名，因为可能无法覆盖
                if (dst.EndsWithIgnoreCase(".exe", ".dll") && File.Exists(dst))
                {
                    // 先尝试删除
                    try
                    {
                        File.Delete(dst);
                    }
                    catch
                    {
                        var del = dst + ".del";
                        if (File.Exists(del)) File.Delete(del);
                        File.Move(dst, del);
                    }
                }

                // 拷贝覆盖
                item.CopyTo(dst.EnsureDirectory(true), true);
            }

            // 删除临时目录
            di.Delete(true);
        }
        #endregion

        #region 日志
        /// <summary>日志对象</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            format = String.Format("[{0}]{1}", Name, format);
            Log?.Info(format, args);
        }
        #endregion
    }
}