using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Web;
using System.Threading;
using System.IO;
using NewLife.Log;
using System.Xml;
using NewLife.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace XCoder
{
    /// <summary>
    /// 自动更新
    /// </summary>
    class AutoUpdate
    {
        #region 属性
        private Version _LocalVersion;
        /// <summary>本地版本</summary>
        public Version LocalVersion
        {
            get { return _LocalVersion; }
            set { _LocalVersion = value; }
        }

        private String _VerSrc;
        /// <summary>版本地址</summary>
        public String VerSrc
        {
            get { return _VerSrc; }
            set { _VerSrc = value; }
        }

        private String _TempPath;
        /// <summary>临时目录</summary>
        public String TempPath
        {
            get { return _TempPath; }
            set { _TempPath = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 执行处理
        /// </summary>
        public void ProcessAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate(Object state)
            {
                try
                {
                    ProcessInternal();
                }
                catch { };
            });
        }

        void ProcessInternal()
        {
            // 取版本
            // 对比版本
            // 拿出更新源
            // 下载更新包
            // 执行更新

            #region 取版本、对比版本
            WebClientX client = new WebClientX();
            // 同步下载，3秒超时
            client.Timeout = 3000;
            String xml = client.DownloadString(VerSrc);
            if (String.IsNullOrEmpty(xml)) return;

            VerFile ver = new VerFile(xml);
            if (LocalVersion >= ver.GetVersion()) return;
            #endregion

            #region 提示更新
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("是否更新到最新版本：{0}", ver.Ver);
                sb.AppendLine();
                if (!String.IsNullOrEmpty(ver.Description))
                {
                    sb.AppendLine("更新内容：");
                    sb.Append(ver.Description);
                }

                if (MessageBox.Show(sb.ToString(), "发现新版本", MessageBoxButtons.OKCancel) == DialogResult.Cancel) return;
            }
            #endregion

            #region 更新
            String upfile = String.Format("XCoder_{0}.zip", ver.Ver);
            upfile = Path.Combine("Update", upfile);
            String file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, upfile);
            String dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (!File.Exists(file))
            {
                String url = ver.Src;
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    Uri uri = new Uri(VerSrc);
                    uri = new Uri(uri, url);
                    url = uri.ToString();
                }
                XTrace.WriteLine("准备从{0}下载相关文件到{1}！", url, file);

                client.DownloadFile(url, file);
            }
            if (File.Exists(file))
            {
                //// 删除旧的Update\Template目录
                //dir = Path.Combine(dir, Path.GetFileNameWithoutExtension(file));
                //if (Directory.Exists(dir)) Directory.Delete(dir, true);

                // 解压缩，删除压缩文件
                IOHelper.DecompressFile(file, null, false);
                File.Delete(file);

                // 复制文件
                String[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                if (files != null && files.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (String item in files)
                    {
                        String ap = item.Substring(dir.Length);
                        if (ap.StartsWith(@"\")) ap = ap.Substring(1);

                        ap = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ap);
                        String ad = Path.GetDirectoryName(ap);
                        if (!Directory.Exists(ad)) Directory.CreateDirectory(ad);

                        //File.Copy(item, ap, true);
                        sb.AppendFormat("xcopy {0} {1} /y /r", item, ap);
                        sb.AppendLine();
                        sb.AppendFormat("del {0} /f /q", item);
                        sb.AppendLine();
                    }

                    // 启动XCoder
                    sb.AppendLine("start " + Application.ExecutablePath);
                    // 删除dir目录
                    sb.AppendLine("rd \"" + dir + "\" /s /q");

                    String tmpfile = Path.GetTempFileName() + ".bat";
                    //String tmpfile = "Update_" + DateTime.Now.ToString("yyMMddHHmmss") + ".bat";
                    //tmpfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tmpfile);
                    File.WriteAllText(tmpfile, sb.ToString(), Encoding.Default);

                    String ph = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NewLife.ProcessHelper.exe");
                    if (File.Exists(ph)) File.Delete(ph);
                    FileSource.ReleaseFile("XCoder.NewLife.ProcessHelper.exe", ph);

                    ProcessStartInfo si = new ProcessStartInfo();
                    si.FileName = ph;
                    si.Arguments = Process.GetCurrentProcess().Id + " " + tmpfile;
                    si.CreateNoWindow = true;
                    si.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(si);

                    //Process.Start(ph, Process.GetCurrentProcess().Id + " " + tmpfile);
                    Application.Exit();
                }

                //// 删除Update\Template目录
                //if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
            #endregion
        }
        #endregion

        #region 版本
        class VerFile
        {
            #region 属性
            private String _Ver;
            /// <summary>版本</summary>
            public String Ver
            {
                get { return _Ver; }
                set { _Ver = value; }
            }

            private String _Src;
            /// <summary>文件源</summary>
            public String Src
            {
                get { return _Src; }
                set { _Src = value; }
            }

            private String _Description;
            /// <summary>描述</summary>
            public String Description
            {
                get { return _Description; }
                set { _Description = value; }
            }
            #endregion

            public VerFile(String xml)
            {
                Parse(xml);
            }

            void Parse(String xml)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                XmlNode node = doc.SelectSingleNode(@"//ver");
                if (node != null) Ver = node.InnerText;
                node = doc.SelectSingleNode(@"//src");
                if (node != null) Src = node.InnerText;
                node = doc.SelectSingleNode(@"//description");
                if (node != null) Description = node.InnerText;
            }

            public Version GetVersion()
            {
                return new Version(Ver);
            }
        }
        #endregion
    }
}
