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

namespace NewLife.Net
{
    /// <summary>升级更新</summary>
    /// <remarks>
    /// 自动更新的难点在于覆盖正在使用的exe/dll文件，通过改名可以解决
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
        public DateTime Compile { get; set; }

        /// <summary>更新完成以后自动启动主程序</summary>
        public Boolean AutoStart { get; set; } = true;

        /// <summary>更新目录</summary>
        public String UpdatePath { get; set; } = "Update";

        /// <summary>超链接信息，其中第一个为最佳匹配项</summary>
        public Link[] Links { get; set; } = new Link[0];

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
            Compile = asmx.Compile;

            Server = NewLife.Setting.Current.PluginServer;
        }
        #endregion

        #region 方法
        /// <summary>获取版本信息，检查是否需要更新</summary>
        /// <returns></returns>
        public Boolean Check()
        {
            // 删除备份文件
            DeleteBuckup();

            var url = Server;
            // 如果配置地址未指定参数，则附加参数
            if (url.StartsWithIgnoreCase("http://"))
            {
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
            }

            WriteLog("准备获取更新信息 {0}", url);

            var web = CreateClient();
            var html = web.GetHtml(url);
            var links = Link.Parse(html, url, item => item.Name.StartsWithIgnoreCase(Name) || item.Name.Contains(Name));
            if (links.Length < 1) return false;

            // 分析所有链接
            var list = new List<Link>();
            foreach (var link in links)
            {
                // 不是满足条件的name不要
                if (!link.Name.StartsWithIgnoreCase(Name) || !link.Name.Contains(Name)) continue;

                // 第一个时间命中
                if (link.Time.Year <= DateTime.Now.Year) list.Add(link);
            }
            if (list.Count < 1) return false;

            // 按照时间降序
            Links = list.OrderByDescending(e => e.Time).ToArray();

            // 只有文件时间大于编译时间才更新，需要考虑文件编译后过一段时间才打包
            return Links[0].Time > Compile.AddMinutes(10);
        }

        /// <summary>开始更新</summary>
        public void Download()
        {
            if (Links.Length == 0) throw new Exception("没有可用新版本！");

            var link = Links[0];
            if (String.IsNullOrEmpty(link.Url)) throw new Exception("升级包地址无效！");

            // 如果更新包不存在，则下载
            var file = UpdatePath.CombinePath(link.Name).GetFullPath();
            if (!File.Exists(file))
            {
                WriteLog("准备下载 {0} 到 {1}", link.Url, file);

                var sw = Stopwatch.StartNew();

                var web = CreateClient();
                Task.Run(() => web.DownloadFileAsync(link.Url, file)).Wait();

                sw.Stop();
                WriteLog("下载完成！大小{0:n0}字节，耗时{1:n0}ms", file.AsFile().Length, sw.ElapsedMilliseconds);
            }

            SourceFile = file;
        }

        /// <summary>检查并执行更新操作</summary>
        public Boolean Update()
        {
            var file = SourceFile;

            if (!File.Exists(file)) return false;
            WriteLog("发现更新包 {0}", file);

            // 解压更新程序包
            if (!file.EndsWithIgnoreCase(".zip")) return false;

            var dest = XTrace.TempPath.CombinePath(Path.GetFileNameWithoutExtension(file)).GetFullPath();
            WriteLog("解压缩更新包到临时目录 {0}", dest);
            file.AsFile().Extract(dest, true);

            // 拷贝替换更新
            CopyAndReplace(dest);

            if (AutoStart)
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

            return true;
        }

        /// <summary>正在使用锁定的文件不可删除，但可以改名</summary>
        /// <param name="source"></param>
        private void CopyAndReplace(String source)
        {
            // 删除备份
            DeleteBuckup();

            var di = source.AsDirectory();

            // 来源目录根，用于截断
            var root = di.FullName.EnsureEnd(Path.DirectorySeparatorChar.ToString());
            foreach (var item in di.GetAllFiles(null, true))
            {
                var name = item.FullName.TrimStart(root);
                var dst = ".".CombinePath(name).GetFullPath();

                // 如果是应用配置文件，不要更新
                if (dst.EndsWithIgnoreCase(".exe.config")) continue;

                WriteLog("更新 {0}", dst);

                // 如果是exe/dll，则先改名，因为可能无法覆盖
                if (dst.EndsWithIgnoreCase(".exe", ".dll")) File.Move(dst, dst + ".del");

                // 拷贝覆盖
                item.CopyTo(dst.EnsureDirectory(true), true);
            }

            // 删除临时目录
            di.Delete(true);
        }
        #endregion

        #region 辅助
        private WebClientX _Client;
        private WebClientX CreateClient()
        {
            if (_Client != null) return _Client;

            var web = new WebClientX(true, true)
            {
                UserAgent = "NewLife.Upgrade"
            };
            return _Client = web;
        }

        /// <summary>删除备份文件</summary>
        public static void DeleteBuckup()
        {
            // 删除备份
            var di = ".".AsDirectory();
            var fs = di.GetAllFiles("*.del");
            foreach (var item in fs)
            {
                item.Delete();
            }
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