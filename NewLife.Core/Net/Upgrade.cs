using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NewLife.Compression;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;
using NewLife.Xml;

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

        private UpgradeVersion _ServerVersion;
        /// <summary>更新版本信息</summary>
        public UpgradeVersion ServerVersion { get { return _ServerVersion; } set { _ServerVersion = value; } }

        private String _TempPath = XTrace.TempPath;
        /// <summary>临时目录</summary>
        public String TempPath { get { return _TempPath; } set { _TempPath = value; } }

        private String _VerFile;
        /// <summary>版本文件</summary>
        public String VerFile { get { return _VerFile; } set { _VerFile = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个升级对象实例，获取当前应用信息</summary>
        public Upgrade()
        {
            var asm = Assembly.GetEntryAssembly();

            Version = asm.GetName().Version;
            Name = asm.GetName().Name;

            // 如果版本文件存在，加载
            VerFile = Name + "Ver.xml";
            VerFile = TempPath.CombinePath(VerFile).GetFullPath();
            if (File.Exists(VerFile))
            {
                try
                {
                    ServerVersion = File.ReadAllText(VerFile).ToXmlEntity<UpgradeVersion>();
                }
                catch { }
            }
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

            var web = CreateClient();
            var html = web.DownloadString(url);
            if (String.IsNullOrEmpty(html)) return false;

            var ver = html.ToXmlEntity<UpgradeVersion>();
            if (ver == null) return false;

            ServerVersion = ver;
            ver.ToXmlFile(VerFile);

            // 检查最低版本要求
            if (ver.MinVersion != null && ver.MinVersion > Version) return false;

            // 检查更新版本
            return ver.Version > Version;
        }

        /// <summary>开始更新</summary>
        public void Start()
        {
            if (ServerVersion == null) throw new Exception("未检查新版本！");
            if (String.IsNullOrEmpty(ServerVersion.Url)) throw new Exception("升级包地址无效！");

            var ver = ServerVersion;

            // 如果更新包不存在，则下载
            var file = TempPath.CombinePath(ver.Name).GetFullPath();
            if (!CheckCrc(file, ver.Crc))
            {
                var web = CreateClient();
                web.DownloadFile(ver.Url, file);
            }

            if (!String.IsNullOrEmpty(ver.Upgrader))
            {
                // 检查并下载更新程序
                file = TempPath.CombinePath(ver.Upgrader).GetFullPath();
                if (!CheckCrc(file, ver.UpgraderCrc))
                {
                    var web = CreateClient();
                    web.DownloadFile(ver.UpgraderUrl, file);
                }

                // 解压更新程序包
                if (file.EndsWithIgnoreCase(".zip"))
                {
                    var p = Path.GetFileNameWithoutExtension(file);
                    ZipFile.Extract(file, p);
                    // 找到第一个exe
                    var f = Directory.GetFiles(p, "*.exe").FirstOrDefault();
                    if (String.IsNullOrEmpty(f)) throw new XException("更新程序包错误，无法找到主程序 {0}", file);
                    file = f;
                }

                // 执行更新程序包
                var si = new ProcessStartInfo();
                si.FileName = file;
                si.WorkingDirectory = Path.GetDirectoryName(si.FileName);
                si.Arguments = "-pid " + Process.GetCurrentProcess().Id + " -xml \"" + VerFile + "\"";
                if (!XTrace.Debug)
                {
                    si.CreateNoWindow = true;
                    si.WindowStyle = ProcessWindowStyle.Hidden;
                }
                si.UseShellExecute = false;
                Process.Start(si);

                XTrace.WriteLine("已启动更新程序来升级，升级配置：{0}", VerFile);

                Application.Exit();
            }
        }
        #endregion

        #region 辅助
        static WebClientX CreateClient()
        {
            var web = new WebClientX(true, true);
            web.UserAgent = "NewLife.Upgrade";
            return web;
        }

        static Boolean CheckCrc(String file, String crc)
        {
            if (!File.Exists(file)) return false;
            if (String.IsNullOrEmpty(crc)) return false;

            var c32 = new Crc32();
            using (var fs = File.OpenRead(file))
            {
                c32.Update(fs);
            }

            return c32.Value.ToString("X8").EqualIgnoreCase(crc);
        }
        #endregion
    }
}