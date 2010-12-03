using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using XCode.DataAccessLayer;
using XTemplate.Templating;
using System.Xml;
using System.Net;

namespace XCoder
{
    public partial class FrmMain : Form
    {
        #region 属性
        /// <summary>
        /// 配置
        /// </summary>
        public static XConfig Config { get { return XConfig.Current; } }

        private XCoder _Coder;
        /// <summary>生成器</summary>
        public XCoder Coder
        {
            get { return _Coder ?? (_Coder = new XCoder(Config)); }
            set { _Coder = value; }
        }
        #endregion

        #region 界面初始化
        public FrmMain()
        {
            InitializeComponent();

            AutoLoadTables(Config.ConnName);

            FileSource.CheckTemplate();
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            Text = "新生命代码生成器 V" + FileVersion;
            Template.BaseClassName = typeof(XCoderBase).FullName;
        }

        private static String _FileVersion;
        /// <summary>
        /// 文件版本
        /// </summary>
        public static String FileVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_FileVersion))
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    AssemblyFileVersionAttribute av = Attribute.GetCustomAttribute(asm, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                    if (av != null) _FileVersion = av.Version;
                    if (String.IsNullOrEmpty(_FileVersion)) _FileVersion = "1.0";
                }
                return _FileVersion;
            }
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            //ConnectionStringSettingsCollection conns = ConfigurationManager.ConnectionStrings;
            //if (conns != null && conns.Count > 0)
            //{
            //    foreach (ConnectionStringSettings item in conns)
            //    {
            //        if (item.Name.Equals("LocalSqlServer", StringComparison.OrdinalIgnoreCase)) continue;
            //        cbConn.Items.Add(item.Name);
            //    }
            //    cbConn.SelectedIndex = 0;
            //}

            List<String> list = new List<String>();
            foreach (String item in DAL.ConnStrs.Keys)
            {
                list.Add(item);
            }
            Conns = list;

            BindTemplate(cb_Template);

            LoadConfig();

            ThreadPool.QueueUserWorkItem(AutoDetectDatabase);
            ThreadPool.QueueUserWorkItem(UpdateArticles);
        }

        /// <summary>
        /// 自动检测数据库，主要针对MSSQL
        /// </summary>
        /// <param name="state"></param>
        void AutoDetectDatabase(Object state)
        {
            List<String> list = new List<String>();
            // 加上本机
            DAL.AddConnStr("localhost", "server=.;Integrated Security=SSPI;Database=master", null, "sql2000");
            foreach (String item in DAL.ConnStrs.Keys)
            {
                list.Add(item);
            }

            String[] sysdbnames = new String[] { "master", "tempdb", "model", "msdb" };

            List<String> names = new List<String>();
            foreach (String item in list)
            {
                try
                {
                    DAL dal = DAL.Create(item);
                    DataSet ds = null;
                    // 列出所有数据库
                    if (dal.DbType == DatabaseType.SqlServer)
                    {
                        ds = dal.Select("SELECT name FROM sysdatabases", "");
                    }
                    else if (dal.DbType == DatabaseType.SqlServer2005)
                    {
                        ds = dal.Select("SELECT name FROM sys.databases", "");
                    }
                    else
                        continue;

                    DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                    builder.ConnectionString = dal.ConnStr;

                    // 统计库名
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        String dbname = dr[0].ToString();
                        if (Array.IndexOf(sysdbnames, dbname) >= 0) continue;

                        String connName = String.Format("{0}_{1}", item, dbname);

                        builder["Database"] = dbname;
                        DAL.AddConnStr(connName, builder.ToString(), null, "sql2000");

                        try
                        {
                            String ver = dal.DB.ServerVersion;
                            names.Add(connName);
                        }
                        catch
                        {
                            if (DAL.ConnStrs.ContainsKey(connName)) DAL.ConnStrs.Remove(connName);
                        }
                    }
                }
                catch
                {
                    if (item == "localhost") DAL.ConnStrs.Remove("localhost");
                }
            }

            if (DAL.ConnStrs.ContainsKey("localhost")) DAL.ConnStrs.Remove("localhost");
            if (list.Contains("localhost")) list.Remove("localhost");

            if (names != null && names.Count > 0)
            {
                list.AddRange(names);

                Conns = list;
            }
        }

        /// <summary>
        /// 连接字符串
        /// </summary>
        List<String> Conns = null;

        /// <summary>
        /// 模版绑定
        /// </summary>
        public void BindTemplate(ComboBox cb)
        {
            String TemplatePath = XCoder.TemplatePath;

            cb.Items.Clear();

            if (!Directory.Exists(TemplatePath))
            {
                MessageBox.Show("模版目录 " + TemplatePath + " 不存在，正在初始化！");
                //Thread.Sleep(3000);
            }

            if (!Directory.Exists(TemplatePath))
            {
                //Directory.CreateDirectory(TemplatePath);
                MessageBox.Show("模版目录 " + TemplatePath + " 不存在，请先添加模版");
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(TemplatePath);
            DirectoryInfo[] dirs = dir.GetDirectories();
            List<String> dirs2 = new List<string>();
            foreach (DirectoryInfo d in dirs)
            {
                if (d.Name != "bin" && d.Name != "obj" && d.Name != "Properties") dirs2.Add(d.Name);
            }
            cb.DataSource = dirs2;
            cb.DisplayMember = "value";
            cb.Update();
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }
        #endregion

        #region 连接
        private void bt_Connection_Click(object sender, EventArgs e)
        {
            SaveConfig();

            if (bt_Connection.Text == "连接")
            {
                cb_Table.Items.Clear();

                Coder = null;
                cb_Table.DataSource = Coder.Tables;
                cb_Table.DisplayMember = "Name";
                cb_Table.ValueMember = "Name";

                groupBox1.Enabled = false;
                groupBox2.Enabled = true;
                bt_Connection.Text = "断开";
            }
            else
            {
                cb_Table.DataSource = null;
                cb_Table.Items.Clear();

                groupBox1.Enabled = true;
                groupBox2.Enabled = false;
                bt_Connection.Text = "连接";
            }
        }

        void AutoLoadTables(String name)
        {
            // 异步加载
            ThreadPool.QueueUserWorkItem(delegate(Object state)
            {
                try
                {
                    IList<XTable> tables = DAL.Create(name).Tables;
                }
                catch { }
            });
        }
        #endregion

        #region 生成
        Stopwatch sw = new Stopwatch();
        private void bt_GenTable_Click(object sender, EventArgs e)
        {
            SaveConfig();

            if (cb_Template.SelectedValue == null || cb_Table.SelectedValue == null) return;

            sw.Reset();
            sw.Start();

            try
            {
                String[] ss = Coder.Render(cb_Table.Text);
                richTextBox1.Text = ss[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            sw.Stop();
            lb_Status.Text = "生成 " + cb_Table.Text + " 完成！耗时：" + sw.Elapsed.ToString();
        }

        private void bt_GenAll_Click(object sender, EventArgs e)
        {
            SaveConfig();

            if (cb_Template.SelectedValue == null || cb_Table.Items.Count < 1) return;

            IList<XTable> tables = Coder.Tables;
            if (tables == null || tables.Count < 1) return;

            pg_Process.Minimum = 0;
            pg_Process.Maximum = tables.Count;
            pg_Process.Step = 1;
            pg_Process.Value = pg_Process.Minimum;

            List<String> param = new List<string>();
            //param.Add(cb_Template.Text);
            //param.Add(txt_OutPath.Text);
            //for (int i = 0; i < cb_Table.Items.Count; i++)
            //{
            //    param.Add(cb_Table.GetItemText(cb_Table.Items[i]));
            //}
            foreach (XTable item in tables)
            {
                param.Add(item.Name);
            }

            bt_GenAll.Enabled = false;

            if (!bw.IsBusy)
            {
                sw.Reset();
                sw.Start();

                bw.RunWorkerAsync(param);
            }
            else
                bw.CancelAsync();
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            List<String> param = e.Argument as List<String>;
            int i = 1;
            foreach (String tableName in param)
            {
                try
                {
                    Coder.Render(tableName);
                }
                catch (Exception ex)
                {
                    bw.ReportProgress(i++, "出错：" + ex.ToString());
                    break;
                }

                bw.ReportProgress(i++, "已生成：" + tableName);
                if (bw.CancellationPending) break;
            }
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pg_Process.Value = e.ProgressPercentage;
            proc_percent.Text = (int)(100 * pg_Process.Value / pg_Process.Maximum) + "%";
            lb_Status.Text = e.UserState.ToString();

            if (lb_Status.Text.StartsWith("出错")) MessageBox.Show(lb_Status.Text, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pg_Process.Value = pg_Process.Maximum;
            proc_percent.Text = (int)(100 * pg_Process.Value / pg_Process.Maximum) + "%";
            Coder = null;

            sw.Stop();
            lb_Status.Text = "生成 " + cb_Table.Items.Count + " 个类完成！耗时：" + sw.Elapsed.ToString();

            bt_GenAll.Enabled = true;
        }
        #endregion

        #region 加载、保存
        public void LoadConfig()
        {
            if (!String.IsNullOrEmpty(Config.ConnName))
            {
                if (String.IsNullOrEmpty(Config.EntityConnName)) Config.EntityConnName = Config.ConnName;
                if (String.IsNullOrEmpty(Config.NameSpace)) Config.NameSpace = Config.ConnName;
                if (String.IsNullOrEmpty(Config.OutputPath)) Config.OutputPath = Config.ConnName;
            }

            cbConn.Text = Config.ConnName;
            cb_Template.Text = Config.TemplateName;
            txt_OutPath.Text = Config.OutputPath;
            txt_NameSpace.Text = Config.NameSpace;
            txt_ConnName.Text = Config.EntityConnName;
            txtPrefix.Text = Config.Prefix;
            checkBox1.Checked = Config.AutoCutPrefix;
            checkBox2.Checked = Config.AutoFixWord;
            checkBox3.Checked = Config.UseCNFileName;
            checkBox5.Checked = Config.UseHeadTemplate;
            richTextBox2.Text = Config.HeadTemplate;
            checkBox4.Checked = Config.Debug;
        }

        public void SaveConfig()
        {
            if (!String.IsNullOrEmpty(Config.ConnName))
            {
                if (String.IsNullOrEmpty(Config.EntityConnName)) Config.EntityConnName = Config.ConnName;
                if (String.IsNullOrEmpty(Config.NameSpace)) Config.NameSpace = Config.ConnName;
                if (String.IsNullOrEmpty(Config.OutputPath)) Config.OutputPath = Config.ConnName;
            }

            Config.ConnName = cbConn.Text;
            Config.TemplateName = cb_Template.Text;
            Config.OutputPath = txt_OutPath.Text;
            Config.NameSpace = txt_NameSpace.Text;
            Config.EntityConnName = txt_ConnName.Text;
            Config.Prefix = txtPrefix.Text;
            Config.AutoCutPrefix = checkBox1.Checked;
            Config.AutoFixWord = checkBox2.Checked;
            Config.UseCNFileName = checkBox3.Checked;
            Config.UseHeadTemplate = checkBox5.Checked;
            Config.HeadTemplate = richTextBox2.Text;
            Config.Debug = checkBox4.Checked;

            Config.Save();
        }
        #endregion

        #region 导出映射文件
        private void button1_Click(object sender, EventArgs e)
        {
            IList<XTable> tables = DAL.Create(Config.ConnName).Tables;
            if (tables == null || tables.Count < 1) return;

            foreach (XTable table in tables)
            {
                XCoder.AddWord(table.Name, table.Description);
                foreach (XField field in table.Fields)
                {
                    XCoder.AddWord(field.Name, field.Description);
                }
            }

            MessageBox.Show("完成！", this.Text);
        }
        #endregion

        #region 自动化
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Conns != null)
            {
                String str = cbConn.Text;

                cbConn.DataSource = Conns;
                cbConn.DisplayMember = "value";
                cbConn.Update();

                Conns = null;
                if (!String.IsNullOrEmpty(str)) cbConn.Text = str;
            }
        }

        private void cbConn_SelectedIndexChanged(object sender, EventArgs e)
        {
            AutoLoadTables(cbConn.Text);

            if (String.IsNullOrEmpty(cb_Template.Text)) cb_Template.Text = cbConn.Text;
            if (String.IsNullOrEmpty(txt_OutPath.Text)) txt_OutPath.Text = cbConn.Text;
            if (String.IsNullOrEmpty(txt_NameSpace.Text)) txt_NameSpace.Text = cbConn.Text;
        }
        #endregion

        #region 附加信息
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Control control = sender as Control;
            if (control == null) return;

            String url = String.Empty;
            if (control.Tag != null) url = control.Tag.ToString();
            if (String.IsNullOrEmpty(url)) url = control.Text;
            if (String.IsNullOrEmpty(url)) return;

            Process.Start(url);
        }

        private void label3_Click(object sender, EventArgs e)
        {
            Clipboard.SetData("10193406", null);
            MessageBox.Show("QQ群号已复制到剪切板！", "提示");
        }

        List<Article> articles = new List<Article>();

        void UpdateArticles(Object state)
        {
            try
            {
                String url = "http://www.cnblogs.com/nnhy/rss";
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(url);

                XmlDocument doc = new XmlDocument();
                doc.Load(stream);

                XmlNodeList nodes = doc.SelectNodes(@"//item");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode item in nodes)
                    {
                        Article entity = new Article();
                        entity.Title = item.SelectSingleNode("title").InnerText;
                        entity.Link = item.SelectSingleNode("link").InnerText;
                        entity.Description = item.SelectSingleNode("description").InnerText;

                        articles.Add(entity);
                    }
                }
            }
            catch { }
        }

        Int32 articleIndex = 0;
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (articles != null && articles.Count > 0)
            {
                if (articleIndex >= articles.Count) articleIndex = 0;
                Article entity = articles[articleIndex];

                linkLabel1.Text = entity.Title;
                linkLabel1.Tag = entity.Link;

                articleIndex++;
            }
        }

        class Article
        {
            public String Title;
            public String Link;
            public String Description;
        }
        #endregion
    }
}