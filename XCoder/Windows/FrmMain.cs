using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XTemplate.Templating;

namespace XCoder
{
    public partial class FrmMain : Form
    {
        #region 属性
        /// <summary>配置</summary>
        public static XConfig Config { get { return XConfig.Current; } }

        private Engine _Engine;
        /// <summary>生成器</summary>
        Engine Engine
        {
            get { return _Engine ?? (_Engine = new Engine(Config)); }
            set { _Engine = value; }
        }
        #endregion

        #region 界面初始化
        public FrmMain()
        {
            InitializeComponent();

            this.Icon = IcoHelper.GetIcon("模型");

            AutoLoadTables(Config.ConnName);
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            //var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            //Text = String.Format("新生命数据模型工具 v{0} {1:HH:mm:ss}编译", asm.CompileVersion, asm.Compile);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            LoadConfig();

            try
            {
                SetDatabaseList(DAL.ConnStrs.Keys.ToList());

                BindTemplate(cb_Template);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            LoadConfig();

            ThreadPoolX.QueueUserWorkItem(AutoDetectDatabase, null);
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SaveConfig();
            }
            catch { }
        }
        #endregion

        #region 连接、自动检测数据库、加载表
        private void bt_Connection_Click(object sender, EventArgs e)
        {
            SaveConfig();

            if (bt_Connection.Text == "连接")
            {
                Engine = null;
                LoadTables();

                gbConnect.Enabled = false;
                gbTable.Enabled = true;
                模型ToolStripMenuItem.Visible = true;
                架构管理SToolStripMenuItem.Visible = true;
                //btnImport.Enabled = false;
                btnImport.Text = "导出模型";
                bt_Connection.Text = "断开";
                btnRefreshTable.Enabled = true;
            }
            else
            {
                SetTables(null);

                gbConnect.Enabled = true;
                gbTable.Enabled = false;
                模型ToolStripMenuItem.Visible = false;
                架构管理SToolStripMenuItem.Visible = false;
                btnImport.Enabled = true;
                btnImport.Text = "导入模型";
                bt_Connection.Text = "连接";
                btnRefreshTable.Enabled = false;
                Engine = null;

                // 断开的时候再取一次，确保下次能及时得到新的
                try
                {
                    var list = DAL.Create(Config.ConnName).Tables;
                }
                catch { }
            }
        }

        private void cbConn_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(cbConn.Text)) toolTip1.SetToolTip(cbConn, DAL.Create(cbConn.Text).ConnStr);

            AutoLoadTables(cbConn.Text);

            if (String.IsNullOrEmpty(cb_Template.Text)) cb_Template.Text = cbConn.Text;
            if (String.IsNullOrEmpty(txt_OutPath.Text)) txt_OutPath.Text = cbConn.Text;
            if (String.IsNullOrEmpty(txt_NameSpace.Text)) txt_NameSpace.Text = cbConn.Text;
        }

        void AutoDetectDatabase()
        {
            var list = new List<String>();

            // 加上本机MSSQL
            String localName = "local_MSSQL";
            String localstr = "Data Source=.;Initial Catalog=master;Integrated Security=True;";
            if (!ContainConnStr(localstr)) DAL.AddConnStr(localName, localstr, null, "mssql");

            var sw = new Stopwatch();
            sw.Start();

            #region 检测本地Access和SQLite
            var n = 0;
            String[] ss = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.*", SearchOption.TopDirectoryOnly);
            foreach (String item in ss)
            {
                String ext = Path.GetExtension(item);
                //if (ext.EqualIC(".exe")) continue;
                //if (ext.EqualIC(".dll")) continue;
                //if (ext.EqualIC(".zip")) continue;
                //if (ext.EqualIC(".rar")) continue;
                //if (ext.EqualIC(".txt")) continue;
                //if (ext.EqualIC(".config")) continue;
                if (ext.EqualIgnoreCase(".exe", ".dll", ".zip", ".rar", ".txt", ".config")) continue;

                try
                {
                    if (DetectFileDb(item)) n++;
                }
                catch (Exception ex) { XTrace.WriteException(ex); }
            }
            #endregion

            sw.Stop();
            XTrace.WriteLine("自动检测文件{0}个，发现数据库{1}个，耗时：{2}！", ss.Length, n, sw.Elapsed);

            foreach (var item in DAL.ConnStrs)
            {
                if (!String.IsNullOrEmpty(item.Value.ConnectionString)) list.Add(item.Key);
            }

            // 远程数据库耗时太长，这里先列出来
            this.Invoke(new Action<List<String>>(SetDatabaseList), list);
            //!!! 必须另外实例化一个列表，否则作为数据源绑定时，会因为是同一个对象而被跳过
            list = new List<String>(list);

            sw.Reset();
            sw.Start();

            #region 探测连接中的其它库
            var sysdbnames = new String[] { "master", "tempdb", "model", "msdb" };
            n = 0;
            var names = new List<String>();
            foreach (var item in list)
            {
                try
                {
                    var dal = DAL.Create(item);
                    if (dal.DbType != DatabaseType.SqlServer) continue;

                    DataTable dt = null;
                    String dbprovider = null;

                    // 列出所有数据库
                    Boolean old = DAL.ShowSQL;
                    DAL.ShowSQL = false;
                    try
                    {
                        if (dal.Db.CreateMetaData().MetaDataCollections.Contains("Databases"))
                        {
                            dt = dal.Db.CreateSession().GetSchema("Databases", null);
                            dbprovider = dal.DbType.ToString();
                        }
                    }
                    finally { DAL.ShowSQL = old; }

                    if (dt == null) continue;

                    var builder = new DbConnectionStringBuilder();
                    builder.ConnectionString = dal.ConnStr;

                    // 统计库名
                    foreach (DataRow dr in dt.Rows)
                    {
                        String dbname = dr[0].ToString();
                        if (Array.IndexOf(sysdbnames, dbname) >= 0) continue;

                        String connName = String.Format("{0}_{1}", item, dbname);

                        builder["Database"] = dbname;
                        DAL.AddConnStr(connName, builder.ToString(), null, dbprovider);
                        n++;

                        try
                        {
                            String ver = dal.Db.ServerVersion;
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
                    if (item == localName) DAL.ConnStrs.Remove(localName);
                }
            }
            #endregion

            sw.Stop();
            XTrace.WriteLine("发现远程数据库{0}个，耗时：{1}！", n, sw.Elapsed);

            if (DAL.ConnStrs.ContainsKey(localName)) DAL.ConnStrs.Remove(localName);
            if (list.Contains(localName)) list.Remove(localName);

            if (names != null && names.Count > 0)
            {
                list.AddRange(names);

                this.Invoke(new Action<List<String>>(SetDatabaseList), list);
            }
        }

        Boolean DetectFileDb(String item)
        {
            String access = "Standard Jet DB";
            String sqlite = "SQLite";

            using (var fs = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length <= 0) return false;

                var reader = new BinaryReader(fs);
                var bts = reader.ReadBytes(sqlite.Length);
                if (bts != null && bts.Length > 0)
                {
                    if (bts[0] == 'S' && bts[1] == 'Q' && Encoding.ASCII.GetString(bts) == sqlite)
                    {
                        var localstr = String.Format("Data Source={0};", item);
                        if (!ContainConnStr(localstr)) DAL.AddConnStr("SQLite_" + Path.GetFileNameWithoutExtension(item), localstr, null, "SQLite");
                        return true;
                    }
                    else if (bts.Length > 5 && bts[4] == 'S' && bts[5] == 't')
                    {
                        fs.Seek(4, SeekOrigin.Begin);
                        bts = reader.ReadBytes(access.Length);
                        if (Encoding.ASCII.GetString(bts) == access)
                        {
                            var localstr = String.Format("Provider=Microsoft.Jet.OLEDB.4.0; Data Source={0};Persist Security Info=False", item);
                            if (!ContainConnStr(localstr)) DAL.AddConnStr("Access_" + Path.GetFileNameWithoutExtension(item), localstr, null, "Access");
                            return true;
                        }
                    }
                }

                if (fs.Length > 20)
                {
                    fs.Seek(16, SeekOrigin.Begin);
                    var ver = reader.ReadInt32();
                    if (ver == 0x73616261 ||
                        ver == 0x002dd714 ||
                        ver == 0x00357b9d ||
                        ver == 0x003d0900
                        )
                    {
                        var localstr = String.Format("Data Source={0};", item);
                        if (!ContainConnStr(localstr)) DAL.AddConnStr("SqlCe_" + Path.GetFileNameWithoutExtension(item), localstr, null, "SqlCe");
                        return true;
                    }
                }
            }

            return false;
        }

        Boolean ContainConnStr(String connstr)
        {
            foreach (var item in DAL.ConnStrs)
            {
                if (connstr.EqualIgnoreCase(item.Value.ConnectionString)) return true;
            }
            return false;
        }

        void SetDatabaseList(List<String> list)
        {
            String str = cbConn.Text;

            cbConn.DataSource = list;
            cbConn.DisplayMember = "value";

            if (!String.IsNullOrEmpty(str)) cbConn.Text = str;

            if (!String.IsNullOrEmpty(Config.ConnName))
            {
                cbConn.SelectedText = Config.ConnName;
            }

            if (cbConn.SelectedIndex < 0 && cbConn.Items != null && cbConn.Items.Count > 0) cbConn.SelectedIndex = 0;
        }

        void LoadTables()
        {
            try
            {
                var list = DAL.Create(Config.ConnName).Tables;
                if (!cbIncludeView.Checked) list = list.Where(t => !t.IsView).ToList();
                if (Config.NeedFix) list = Engine.FixTable(list);
                Engine.Tables = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Text);
                return;
            }

            SetTables(null);
            SetTables(Engine.Tables);
        }

        void SetTables(Object source)
        {
            if (source == null)
            {
                cbTableList.DataSource = source;
                cbTableList.Items.Clear();
                return;
            }
            var list = source as List<IDataTable>;
            if (list[0].DbType == DatabaseType.SqlServer) // 增加对SqlServer 2000的特殊处理  ahuang
            {
                //list.Remove(list.Find(delegate(IDataTable p) { return p.Name == "dtproperties"; }));
                //list.Remove(list.Find(delegate(IDataTable p) { return p.Name == "sysconstraints"; }));
                //list.Remove(list.Find(delegate(IDataTable p) { return p.Name == "syssegments"; }));
                //list.RemoveAll(delegate(IDataTable p) { return p.Description.Contains("[0E232FF0-B466-"); });
                list.RemoveAll(dt => dt.Name == "dtproperties" || dt.Name == "sysconstraints" || dt.Name == "syssegments" || dt.Description.Contains("[0E232FF0-B466-"));
            }

            // 设置前最好清空，否则多次设置数据源会用第一次绑定控件，然后实际数据是最后一次
            //cbTableList.DataSource = source;
            cbTableList.Items.Clear();
            if (source != null)
            {
                // 表名排序
                var tables = source as List<IDataTable>;
                if (tables == null)
                    cbTableList.DataSource = source;
                else
                {
                    tables.Sort((t1, t2) => t1.Name.CompareTo(t2.Name));
                    cbTableList.DataSource = tables;
                }
                ////cbTableList.DisplayMember = "Name";
                //cbTableList.ValueMember = "Name";
            }
            cbTableList.Update();
        }

        void AutoLoadTables(String name)
        {
            if (String.IsNullOrEmpty(name)) return;
            //if (!DAL.ConnStrs.ContainsKey(name) || String.IsNullOrEmpty(DAL.ConnStrs[name].ConnectionString)) return;
            ConnectionStringSettings setting;
            if (!DAL.ConnStrs.TryGetValue(name, out setting) || setting.ConnectionString.IsNullOrWhiteSpace()) return;

            // 异步加载
            ThreadPoolX.QueueUserWorkItem(delegate(Object state) { IList<IDataTable> tables = DAL.Create((String)state).Tables; }, name, null);
        }

        private void btnRefreshTable_Click(object sender, EventArgs e)
        {
            LoadTables();
        }
        #endregion

        #region 生成
        Stopwatch sw = new Stopwatch();
        private void bt_GenTable_Click(object sender, EventArgs e)
        {
            SaveConfig();

            if (cb_Template.SelectedValue == null || cbTableList.SelectedValue == null) return;

            var table = cbTableList.SelectedValue as IDataTable;
            if (table == null) return;

            sw.Reset();
            sw.Start();

            try
            {
                var ss = Engine.Render(table);

                MessageBox.Show("生成" + table + "成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (TemplateException ex)
            {
                MessageBox.Show(ex.Message, "模版错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            sw.Stop();
            lb_Status.Text = "生成 " + cbTableList.Text + " 完成！耗时：" + sw.Elapsed;
        }

        private void bt_GenAll_Click(object sender, EventArgs e)
        {
            SaveConfig();

            if (cb_Template.SelectedValue == null || cbTableList.Items.Count < 1) return;

            var tables = Engine.Tables;
            if (tables == null || tables.Count < 1) return;

            sw.Reset();
            sw.Start();

            foreach (var tb in tables)
            {
                Engine.Render(tb);
            }

            sw.Stop();
            lb_Status.Text = "生成 " + tables.Count + " 个类完成！耗时：" + sw.Elapsed.ToString();

            MessageBox.Show("生成" + tables.Count + " 个类成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region 加载、保存
        public void LoadConfig()
        {
            cbConn.Text = Config.ConnName;
            cb_Template.Text = Config.TemplateName;
            txt_OutPath.Text = Config.OutputPath;
            txt_NameSpace.Text = Config.NameSpace;
            txt_ConnName.Text = Config.EntityConnName;
            txtBaseClass.Text = Config.BaseClass;
            cbRenderGenEntity.Checked = Config.RenderGenEntity;

            checkBox3.Checked = Config.UseCNFileName;
            checkBox5.Checked = Config.UseHeadTemplate;
            //richTextBox2.Text = Config.HeadTemplate;
            checkBox4.Checked = Config.Debug;
        }

        public void SaveConfig()
        {
            Config.ConnName = cbConn.Text;
            Config.TemplateName = cb_Template.Text;
            Config.OutputPath = txt_OutPath.Text;
            Config.NameSpace = txt_NameSpace.Text;
            Config.EntityConnName = txt_ConnName.Text;
            Config.BaseClass = txtBaseClass.Text;
            Config.RenderGenEntity = cbRenderGenEntity.Checked;

            Config.UseCNFileName = checkBox3.Checked;
            Config.UseHeadTemplate = checkBox5.Checked;
            //Config.HeadTemplate = richTextBox2.Text;
            Config.Debug = checkBox4.Checked;

            Config.Save();
        }
        #endregion

        #region 附加信息
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var control = sender as Control;
            if (control == null) return;

            String url = String.Empty;
            if (control.Tag != null) url = control.Tag.ToString();
            if (String.IsNullOrEmpty(url)) url = control.Text;
            if (String.IsNullOrEmpty(url)) return;

            Process.Start(url);
        }

        private void label3_Click(object sender, EventArgs e)
        {
            Clipboard.SetData("1600800", null);
            MessageBox.Show("QQ群号已复制到剪切板！", "提示");
        }
        #endregion

        #region 打开输出目录
        private void btnOpenOutputDir_Click(object sender, EventArgs e)
        {
            var dir = txt_OutPath.Text.GetFullPath();
            if (!Directory.Exists(dir)) dir = AppDomain.CurrentDomain.BaseDirectory;

            Process.Start("explorer.exe", "\"" + dir + "\"");
            //Process.Start("explorer.exe", "/root,\"" + dir + "\"");
            //Process.Start("explorer.exe", "/select," + dir);
        }

        private void frmItems_Click(object sender, EventArgs e)
        {
            //FrmItems.Create(XConfig.Current.Items).Show();

            FrmItems.Create(XConfig.Current).Show();
        }
        #endregion

        #region 模版相关
        public void BindTemplate(ComboBox cb)
        {
            var list = new List<String>();
            foreach (var item in Engine.FileTemplates)
            {
                list.Add("[文件]" + item);
            }
            foreach (String item in Engine.Templates.Keys)
            {
                String[] ks = item.Split('.');
                if (ks == null || ks.Length < 1) continue;

                String name = "[内置]" + ks[0];
                if (!list.Contains(name)) list.Add(name);
            }
            cb.Items.Clear();
            cb.DataSource = list;
            cb.DisplayMember = "value";
            cb.Update();
        }

        private void btnRelease_Click(object sender, EventArgs e)
        {
            try
            {
                Source.ReleaseAllTemplateFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void lbEditHeader_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = FrmText.Create("C#文件头模版", Config.HeadTemplate);
            frm.ShowDialog();
            Config.HeadTemplate = frm.Content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            frm.Dispose();
        }
        #endregion

        #region 菜单
        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Application.Exit();
            this.Close();
        }

        private void 组件手册ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = "X组件手册.chm";
            if (!File.Exists(file)) file = Path.Combine(@"C:\X\DLL", file);
            if (File.Exists(file)) Process.Start(file);
        }

        private void 表名字段名命名规范ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmText.Create("表名字段名命名规范", Source.GetText("数据库命名规范")).Show();
        }

        private void 在线帮助文档ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.NewLifeX.com/showtopic-260.aspx?r=XCoder_v" + AssemblyX.Create(Assembly.GetExecutingAssembly()).Version);
        }

        private void 检查更新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XConfig.Current.LastUpdate = DateTime.Now;

            try
            {
                var au = new AutoUpdate();
                au.Update();

                MessageBox.Show("没有可用更新！", "自动更新");
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                MessageBox.Show("更新失败！" + ex.Message, "自动更新");
            }
        }

        private void 关于ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FrmText.Create("升级历史", Source.GetText("UpdateInfo")).Show();
        }

        private void 博客ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://nnhy.cnblogs.com");
        }

        private void qQ群1600800ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.NewLifeX.com/?r=XCoder_v" + AssemblyX.Create(Assembly.GetExecutingAssembly()).Version);
        }

        private void oracle客户端运行时检查ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ThreadPoolX.QueueUserWorkItem(CheckOracle);
        }
        void CheckOracle()
        {
            if (!DAL.ConnStrs.ContainsKey("Oracle")) return;

            try
            {
                var list = DAL.Create("Oracle").Tables;

                MessageBox.Show("Oracle客户端运行时检查通过！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oracle客户端运行时检查失败！也可能是用户名密码错误！" + ex.ToString());
            }
        }

        private void 自动格式化设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmFix.Create(Config).ShowDialog();
        }
        #endregion

        #region 模型管理
        private void 模型管理MToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tables = Engine.Tables;
            if (tables == null || tables.Count < 1) return;

            FrmModel.Create(tables).Show();
        }

        private void 导出模型EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tables = Engine.Tables;
            if (tables == null || tables.Count < 1)
            {
                MessageBox.Show(this.Text, "数据库架构为空！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!String.IsNullOrEmpty(Config.ConnName))
            {
                var file = Config.ConnName + ".xml";
                String dir = null;
                if (!String.IsNullOrEmpty(saveFileDialog1.FileName))
                    dir = Path.GetDirectoryName(saveFileDialog1.FileName);
                if (String.IsNullOrEmpty(dir)) dir = AppDomain.CurrentDomain.BaseDirectory;
                //saveFileDialog1.FileName = Path.Combine(dir, file);
                saveFileDialog1.InitialDirectory = dir;
                saveFileDialog1.FileName = file;
            }
            if (saveFileDialog1.ShowDialog() != DialogResult.OK || String.IsNullOrEmpty(saveFileDialog1.FileName)) return;
            try
            {
                String xml = DAL.Export(tables);
                File.WriteAllText(saveFileDialog1.FileName, xml);

                MessageBox.Show("导出架构成功！", "导出架构", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void 架构管理SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String connName = "" + cbConn.SelectedValue;
            if (String.IsNullOrEmpty(connName)) return;

            FrmSchema.Create(DAL.Create(connName).Db).Show();
        }

        private void sQL查询器QToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String connName = "" + cbConn.SelectedValue;
            if (String.IsNullOrEmpty(connName)) return;

            FrmQuery.Create(DAL.Create(connName)).Show();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Text == "导出模型")
            {
                导出模型EToolStripMenuItem_Click(null, EventArgs.Empty);
                return;
            }

            if (openFileDialog1.ShowDialog() != DialogResult.OK || String.IsNullOrEmpty(openFileDialog1.FileName)) return;
            try
            {
                var list = DAL.Import(File.ReadAllText(openFileDialog1.FileName));
                if (!cbIncludeView.Checked) list = list.Where(t => !t.IsView).ToList();
                if (Config.NeedFix) list = Engine.FixTable(list);

                Engine = null;
                Engine.Tables = list;

                SetTables(list);

                gbTable.Enabled = true;
                模型ToolStripMenuItem.Visible = true;
                架构管理SToolStripMenuItem.Visible = false;

                MessageBox.Show("导入架构成功！共" + (list == null ? 0 : list.Count) + "张表！", "导入架构", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        #endregion

        #region 网页
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // 网页加载完成后，自动向下滚动一段距离，越过头部
            webBrowser1.Document.Window.ScrollTo(0, 90);
        }
        #endregion

        #region 添加模型-@宁波-小董 2013
        private void 添加模型ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewModel.CreateForm().Show();
        }
        #endregion
    }
}