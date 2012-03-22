using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using NewLife.Compression;
using NewLife.Log;
using NewLife.Threading;
using NewLife.Web;
using System.Net;

namespace XCoder
{
    /// <summary>自动更新</summary>
    class AutoUpdate
    {
        #region 属性
        private String _VerSrc = "http://j.nnhy.org/?ID=1&f=XCoderVer.xml";
        /// <summary>版本地址</summary>
        public String VerSrc { get { return _VerSrc; } set { _VerSrc = value; } }

        const String verfile = "XCoderVer.xml";
        #endregion

        #region 方法
        /// <summary>执行处理</summary>
        public void UpdateAsync() { ThreadPoolX.QueueUserWorkItem(Update); }

        public void Update()
        {
            // 取版本
            // 对比版本
            // 拿出更新源
            // 下载更新包
            // 执行更新

            #region 准备工作
            var curdir = AppDomain.CurrentDomain.BaseDirectory;

            String ProcessHelper = Path.Combine(curdir, "NewLife.ProcessHelper.exe");
            if (File.Exists(ProcessHelper)) File.Delete(ProcessHelper);

            // 删除Update目录，避免重复使用错误的升级文件
            var update = Path.Combine(curdir, "Update");
            if (Directory.Exists(update)) { try { Directory.Delete(update, true); } catch { } }
            var updatebat = Path.Combine(curdir, "Update.bat");
            if (File.Exists(updatebat)) { try { File.Delete(updatebat); } catch { } }

            // 开发环境下，自动生成版本文件
            if (IsDevelop()) MakeVerFile();
            #endregion

            #region 取版本、对比版本
            var client = new WebClientX();
            client.Encoding = Encoding.UTF8;
            // 同步下载，3秒超时
            client.Timeout = 3000;
            XTrace.WriteLine("准备从{0}下载版本文件！", VerSrc);
            String xml = client.DownloadString(VerSrc);
            if (String.IsNullOrEmpty(xml))
            {
                XTrace.WriteLine("无法从{0}获取版本信息！", VerSrc);
                return;
            }

            var ver = new VerFile(xml);
            var asm = Assembly.GetExecutingAssembly();
            if (asm.GetName().Version >= ver.GetVersion()) return;
            #endregion

            #region 提示更新
            {
                var sb = new StringBuilder();
                sb.AppendFormat("是否更新到最新版本：{0}", ver.Ver);
                sb.AppendLine();
                sb.AppendLine("XCoder会自动关闭以进行更新，更新完成后会启动新版本！");
                if (!String.IsNullOrEmpty(ver.Description))
                {
                    sb.AppendLine("更新内容：");
                    sb.Append(ver.Description);
                }

                if (MessageBox.Show(sb.ToString(), "发现新版本", MessageBoxButtons.OKCancel) == DialogResult.Cancel) return;
            }
            #endregion

            #region 下载
            String file = Path.Combine(update, "XCoder.zip");
            CheckAndDownload(client, file, ver.Src);

            // 检查是否需要更新源码
            String xfile = Path.Combine(@"C:\X\Src", "Src.zip");
            String dfile = Path.Combine(@"C:\X\DLL", "DLL.zip");

            CheckAndDownload(client, xfile, ver.XSrc);
            CheckAndDownload(client, dfile, ver.DLL);
            #endregion

            #region 更新
            // 先更新源代码
            Extract(xfile, null);
            Extract(dfile, null);

            var dir = Path.GetDirectoryName(file);
            if (Extract(file, dir))
            {
                var sb = new StringBuilder();
                // 复制
                sb.AppendFormat("copy \"{0}\\*.*\" \"{1}\" /y", dir, curdir);
                sb.AppendLine();
                sb.AppendLine("rd \"" + dir + "\" /s /q");

                // 启动XCoder
                sb.AppendFormat("start {0}", Path.GetFileName(Application.ExecutablePath));
                sb.AppendLine();
                // 删除dir目录
                //sb.AppendLine("rd \"" + dir + "\" /s /q");

                sb.AppendFormat("del \"{0}\" /f/q", ProcessHelper);
                sb.AppendLine();
                sb.AppendFormat("del \"{0}\" /f/q", updatebat);
                sb.AppendLine();

                //String tmpfile = Path.GetTempFileName() + ".bat";
                //String tmpfile = "Update.bat";
                File.WriteAllText(updatebat, sb.ToString(), Encoding.Default);

                FileSource.ReleaseFile("XCoder.NewLife.ProcessHelper.exe", ProcessHelper);

                ProcessStartInfo si = new ProcessStartInfo();
                si.FileName = ProcessHelper;
                si.Arguments = Process.GetCurrentProcess().Id + " " + updatebat;
                if (!XTrace.Debug)
                {
                    si.CreateNoWindow = true;
                    si.WindowStyle = ProcessWindowStyle.Hidden;
                }
                Process.Start(si);

                XTrace.WriteLine("已启动进程助手来升级，升级脚本：{0}", updatebat);

                Application.Exit();
            }
            #endregion
        }

        static Boolean IsDevelop(String path = null)
        {
            var svn = Path.Combine(path ?? AppDomain.CurrentDomain.BaseDirectory, ".svn");
            return Directory.Exists(svn);
        }

        void MakeVerFile()
        {
            var asm = Assembly.GetExecutingAssembly();
            var stream = asm.GetManifestResourceStream(this.GetType(), "UpdateInfo.txt");
            var sb = new StringBuilder();
            // 只要前五行
            sb.AppendLine();
            using (var reader = new StreamReader(stream))
            {
                for (int i = 0; i < 5 && !reader.EndOfStream; i++)
                {
                    sb.AppendLine(reader.ReadLine());
                }
            }

            var mver = new VerFile();
            mver.Ver = asm.GetName().Version.ToString();
            mver.Src = VerSrc.Replace(verfile, "XCoder.zip");
            mver.XSrc = VerSrc.Replace(verfile, "Src.zip");
            mver.DLL = VerSrc.Replace(verfile, "DLL.zip");
            mver.Description = sb.ToString();

            var mxml = mver.GetXml();
            File.WriteAllText(verfile, mxml);
        }

        void CheckAndDownload(WebClient client, String file, String url)
        {
            if (File.Exists(file) || String.IsNullOrEmpty(url)) return;

            String dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            else if (IsDevelop(dir))
                return;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = VerSrc.Replace(verfile, url);

            XTrace.WriteLine("准备从{0}下载相关文件到{1}！", url, file);

            client.DownloadFile(url, file);
        }

        Boolean Extract(String file, String path)
        {
            if (!File.Exists(file)) return false;
            if (String.IsNullOrEmpty(path)) path = Path.GetDirectoryName(file);

            // 解压缩，删除压缩文件
            XTrace.WriteLine("解压缩{0}到{1}！", file, path);
            try
            {
                ZipFile.Extract(file, path);
            }
            catch (Exception ex) { XTrace.WriteException(ex); }
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) { XTrace.WriteException(ex); return false; }

            return true;
        }
        #endregion

        #region 版本
        class VerFile
        {
            #region 属性
            private String _Ver;
            /// <summary>版本</summary>
            public String Ver { get { return _Ver; } set { _Ver = value; } }

            private String _Src;
            /// <summary>文件源</summary>
            public String Src { get { return _Src; } set { _Src = value; } }

            private String _XSrc;
            /// <summary>X组件源代码</summary>
            public String XSrc { get { return _XSrc; } set { _XSrc = value; } }

            private String _DLL;
            /// <summary>X组件目录</summary>
            public String DLL { get { return _DLL; } set { _DLL = value; } }

            private String _Description;
            /// <summary>描述</summary>
            public String Description { get { return _Description; } set { _Description = value; } }
            #endregion

            public VerFile() { }

            public VerFile(String xml)
            {
                try
                {
                    Parse(xml);
                }
                catch (Exception ex)
                {
                    ex.Data["xml"] = xml;
                    XTrace.WriteLine(xml);

                    throw;
                }
            }

            void Parse(String xml)
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                XmlNode node = doc.SelectSingleNode(@"//ver");
                if (node != null) Ver = node.InnerText;
                node = doc.SelectSingleNode(@"//src");
                if (node != null) Src = node.InnerText;
                node = doc.SelectSingleNode(@"//xsrc");
                if (node != null) XSrc = node.InnerText;
                node = doc.SelectSingleNode(@"//dll");
                if (node != null) DLL = node.InnerText;
                node = doc.SelectSingleNode(@"//description");
                if (node != null) Description = node.InnerText;
            }

            public Version GetVersion() { return new Version(Ver); }

            public String GetXml()
            {
                var doc = new XmlDocument();
                var root = doc.CreateElement("r");
                doc.AppendChild(root);

                var node = doc.CreateElement("ver");
                node.InnerText = Ver;
                root.AppendChild(node);

                node = doc.CreateElement("src");
                node.InnerText = Src;
                root.AppendChild(node);

                node = doc.CreateElement("xsrc");
                node.InnerText = XSrc;
                root.AppendChild(node);

                node = doc.CreateElement("dll");
                node.InnerText = DLL;
                root.AppendChild(node);

                node = doc.CreateElement("description");
                node.InnerText = Description;
                root.AppendChild(node);

                return doc.OuterXml;
            }
        }
        #endregion
    }
}