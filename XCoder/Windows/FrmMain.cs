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
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XTemplate.Templating;

namespace XCoder
{
    public partial class FrmMain : Form
    {
        #region 灞炴€?
        /// <summary>閰嶇疆</summary>
        public static XConfig Config { get { return XConfig.Current; } }

        private Engine _Engine;
        /// <summary>鐢熸垚鍣?/summary>
        Engine Engine
        {
            get { return _Engine ?? (_Engine = new Engine(Config)); }
            set { _Engine = value; }
        }
        #endregion

        #region 鐣岄潰鍒濆鍖?
        public FrmMain()
        {
            InitializeComponent();

            this.Icon = IcoHelper.GetIcon("妯″瀷");

            AutoLoadTables(Config.ConnName);
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            //var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            //Text = String.Format("鏂扮敓鍛芥暟鎹ā鍨嬪伐鍏?v{0} {1:HH:mm:ss}缂栬瘧", asm.CompileVersion, asm.Compile);
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

        #region 杩炴帴銆佽嚜鍔ㄦ娴嬫暟鎹簱銆佸姞杞借〃
        private void bt_Connection_Click(object sender, EventArgs e)
        {
            SaveConfig();

            if (bt_Connection.Text == "杩炴帴")
            {
                Engine = null;
                LoadTables();

                gbConnect.Enabled = false;
                gbTable.Enabled = true;
                妯″瀷ToolStripMenuItem.Visible = true;
                鏋舵瀯绠＄悊SToolStripMenuItem.Visible = true;
                //btnImport.Enabled = false;
                btnImport.Text = "瀵煎嚭妯″瀷";
                bt_Connection.Text = "鏂紑";
                btnRefreshTable.Enabled = true;
            }
            else
            {
                SetTables(null);

                gbConnect.Enabled = true;
                gbTable.Enabled = false;
                妯″瀷ToolStripMenuItem.Visible = false;
                鏋舵瀯绠＄悊SToolStripMenuItem.Visible = false;
                btnImport.Enabled = true;
                btnImport.Text = "瀵煎叆妯″瀷";
                bt_Connection.Text = "杩炴帴";
                btnRefreshTable.Enabled = false;
                Engine = null;

                // 鏂紑鐨勬椂鍊欏啀鍙栦竴娆★紝纭繚涓嬫鑳藉強鏃跺緱鍒版柊鐨?
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

            // 鍔犱笂鏈満MSSQL
            String localName = "local_MSSQL";
            String localstr = "Data Source=.;Initial Catalog=master;Integrated Security=True;";
            if (!ContainConnStr(localstr)) DAL.AddConnStr(localName, localstr, null, "mssql");

            var sw = new Stopwatch();
            sw.Start();

            #region 妫€娴嬫湰鍦癆ccess鍜孲QLite
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
            XTrace.WriteLine("鑷姩妫€娴嬫枃浠秢0}涓紝鍙戠幇鏁版嵁搴搟1}涓紝鑰楁椂锛歿2}锛?, ss.Length, n, sw.Elapsed);

            foreach (var item in DAL.ConnStrs)
            {
                if (!String.IsNullOrEmpty(item.Value.ConnectionString)) list.Add(item.Key);
            }

            // 杩滅▼鏁版嵁搴撹€楁椂澶暱锛岃繖閲屽厛鍒楀嚭鏉?
            this.Invoke(SetDatabaseList, list);
            //!!! 蹇呴』鍙﹀瀹炰緥鍖栦竴涓垪琛紝鍚﹀垯浣滀负鏁版嵁婧愮粦瀹氭椂锛屼細鍥犱负鏄悓涓€涓璞¤€岃璺宠繃
            list = new List<String>(list);

            sw.Reset();
            sw.Start();

            #region 鎺㈡祴杩炴帴涓殑鍏跺畠搴?
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

                    // 鍒楀嚭鎵€鏈夋暟鎹簱
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

                    // 缁熻搴撳悕
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
            XTrace.WriteLine("鍙戠幇杩滅▼鏁版嵁搴搟0}涓紝鑰楁椂锛歿1}锛?, n, sw.Elapsed);

            if (DAL.ConnStrs.ContainsKey(localName)) DAL.ConnStrs.Remove(localName);
            if (list.Contains(localName)) list.Remove(localName);

            if (names != null && names.Count > 0)
            {
                list.AddRange(names);

                this.Invoke(SetDatabaseList, list);
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
            if (list[0].DbType == DatabaseType.SqlServer) // 澧炲姞瀵筍qlServer 2000鐨勭壒娈婂鐞? ahuang
            {
                //list.Remove(list.Find(delegate(IDataTable p) { return p.Name == "dtproperties"; }));
                //list.Remove(list.Find(delegate(IDataTable p) { return p.Name == "sysconstraints"; }));
                //list.Remove(list.Find(delegate(IDataTable p) { return p.Name == "syssegments"; }));
                //list.RemoveAll(delegate(IDataTable p) { return p.Description.Contains("[0E232FF0-B466-"); });
                list.RemoveAll(dt => dt.Name == "dtproperties" || dt.Name == "sysconstraints" || dt.Name == "syssegments" || dt.Description.Contains("[0E232FF0-B466-"));
            }

            // 璁剧疆鍓嶆渶濂芥竻绌猴紝鍚﹀垯澶氭璁剧疆鏁版嵁婧愪細鐢ㄧ涓€娆＄粦瀹氭帶浠讹紝鐒跺悗瀹為檯鏁版嵁鏄渶鍚庝竴娆?
            //cbTableList.DataSource = source;
            cbTableList.Items.Clear();
            if (source != null)
            {
                // 琛ㄥ悕鎺掑簭
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

            // 寮傛鍔犺浇
            ThreadPoolX.QueueUserWorkItem(delegate(Object state) { IList<IDataTable> tables = DAL.Create((String)state).Tables; }, name, null);
        }

        private void btnRefreshTable_Click(object sender, EventArgs e)
        {
            LoadTables();
        }
        #endregion

        #region 鐢熸垚
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

                MessageBox.Show("鐢熸垚" + table + "鎴愬姛锛?, "鎴愬姛", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (TemplateException ex)
            {
                MessageBox.Show(ex.Message, "妯＄増閿欒", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            sw.Stop();
            lb_Status.Text = "鐢熸垚 " + cbTableList.Text + " 瀹屾垚锛佽€楁椂锛? + sw.Elapsed;
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
            lb_Status.Text = "鐢熸垚 " + tables.Count + " 涓被瀹屾垚锛佽€楁椂锛? + sw.Elapsed.ToString();

            MessageBox.Show("鐢熸垚" + tables.Count + " 涓被鎴愬姛锛?, "鎴愬姛", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region 鍔犺浇銆佷繚瀛?
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

        #region 闄勫姞淇℃伅
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
            MessageBox.Show("QQ缇ゅ彿宸插鍒跺埌鍓垏鏉匡紒", "鎻愮ず");
        }
        #endregion

        #region 鎵撳紑杈撳嚭鐩綍
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

        #region 妯＄増鐩稿叧
        public void BindTemplate(ComboBox cb)
        {
            var list = new List<String>();
            foreach (var item in Engine.FileTemplates)
            {
                list.Add("[鏂囦欢]" + item);
            }
            foreach (String item in Engine.Templates.Keys)
            {
                String[] ks = item.Split('.');
                if (ks == null || ks.Length < 1) continue;

                String name = "[鍐呯疆]" + ks[0];
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
            var frm = FrmText.Create("C#鏂囦欢澶存ā鐗?, Config.HeadTemplate);
            frm.ShowDialog();
            Config.HeadTemplate = frm.Content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            frm.Dispose();
        }
        #endregion

        #region 鑿滃崟
        private void 閫€鍑篨ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Application.Exit();
            this.Close();
        }

        private void 缁勪欢鎵嬪唽ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = "X缁勪欢鎵嬪唽.chm";
            if (!File.Exists(file)) file = Path.Combine(@"C:\X\DLL", file);
            if (File.Exists(file)) Process.Start(file);
        }

        private void 琛ㄥ悕瀛楁鍚嶅懡鍚嶈鑼僒oolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmText.Create("琛ㄥ悕瀛楁鍚嶅懡鍚嶈鑼?, Source.GetText("鏁版嵁搴撳懡鍚嶈鑼?)).Show();
        }

        private void 鍦ㄧ嚎甯姪鏂囨。ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.NewLifeX.com/showtopic-260.aspx?r=XCoder_v" + AssemblyX.Create(Assembly.GetExecutingAssembly()).Version);
        }

        private void 妫€鏌ユ洿鏂癟oolStripMenuItem_Click(object sender, EventArgs e)
        {
            XConfig.Current.LastUpdate = DateTime.Now;

            try
            {
                var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var up = new Upgrade();
                if (XConfig.Current.Debug) up.Log = XTrace.Log;
                up.Name = "XCoder";
                up.Server = "http://www.newlifex.com/showtopic-260.aspx";
                up.UpdatePath = root.CombinePath(up.UpdatePath);
                if (up.Check())
                {
                    up.Download();
                    up.Update();
                }
                else if (up.Links != null && up.Links.Length > 0)
                    MessageBox.Show("娌℃湁鍙敤鏇存柊锛佹渶鏂皗0}".F(up.Links[0].Time), "鑷姩鏇存柊");
                else
                    MessageBox.Show("娌℃湁鍙敤鏇存柊锛?, "鑷姩鏇存柊");
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                MessageBox.Show("鏇存柊澶辫触锛? + ex.Message, "鑷姩鏇存柊");
            }
        }

        private void 鍏充簬ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FrmText.Create("鍗囩骇鍘嗗彶", Source.GetText("UpdateInfo")).Show();
        }

        private void 鍗氬ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://nnhy.cnblogs.com");
        }

        private void qQ缇?600800ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.NewLifeX.com/?r=XCoder_v" + AssemblyX.Create(Assembly.GetExecutingAssembly()).Version);
        }

        private void oracle瀹㈡埛绔繍琛屾椂妫€鏌oolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ThreadPoolX.QueueUserWorkItem(CheckOracle);
        }
        void CheckOracle()
        {
            if (!DAL.ConnStrs.ContainsKey("Oracle")) return;

            try
            {
                var list = DAL.Create("Oracle").Tables;

                MessageBox.Show("Oracle瀹㈡埛绔繍琛屾椂妫€鏌ラ€氳繃锛?);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oracle瀹㈡埛绔繍琛屾椂妫€鏌ュけ璐ワ紒涔熷彲鑳芥槸鐢ㄦ埛鍚嶅瘑鐮侀敊璇紒" + ex.ToString());
            }
        }

        private void 鑷姩鏍煎紡鍖栬缃甌oolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmFix.Create(Config).ShowDialog();
        }
        #endregion

        #region 妯″瀷绠＄悊
        private void 妯″瀷绠＄悊MToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tables = Engine.Tables;
            if (tables == null || tables.Count < 1) return;

            FrmModel.Create(tables).Show();
        }

        private void 瀵煎嚭妯″瀷EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tables = Engine.Tables;
            if (tables == null || tables.Count < 1)
            {
                MessageBox.Show(this.Text, "鏁版嵁搴撴灦鏋勪负绌猴紒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                MessageBox.Show("瀵煎嚭鏋舵瀯鎴愬姛锛?, "瀵煎嚭鏋舵瀯", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void 鏋舵瀯绠＄悊SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String connName = "" + cbConn.SelectedValue;
            if (String.IsNullOrEmpty(connName)) return;

            FrmSchema.Create(DAL.Create(connName).Db).Show();
        }

        private void sQL鏌ヨ鍣≦ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String connName = "" + cbConn.SelectedValue;
            if (String.IsNullOrEmpty(connName)) return;

            FrmQuery.Create(DAL.Create(connName)).Show();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Text == "瀵煎嚭妯″瀷")
            {
                瀵煎嚭妯″瀷EToolStripMenuItem_Click(null, EventArgs.Empty);
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
                妯″瀷ToolStripMenuItem.Visible = true;
                鏋舵瀯绠＄悊SToolStripMenuItem.Visible = false;

                MessageBox.Show("瀵煎叆鏋舵瀯鎴愬姛锛佸叡" + (list == null ? 0 : list.Count) + "寮犺〃锛?, "瀵煎叆鏋舵瀯", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        #endregion

        #region 缃戦〉
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // 缃戦〉鍔犺浇瀹屾垚鍚庯紝鑷姩鍚戜笅婊氬姩涓€娈佃窛绂伙紝瓒婅繃澶撮儴
            webBrowser1.Document.Window.ScrollTo(0, 90);
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url != null)
            {
                var url = e.Url.ToString();
                if (!url.IsNullOrWhiteSpace())
                {
                    // 绮剧畝鐗堟浛鎹负瀹屾暣鐗?
                    var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
                    url = url.Replace("/archiver/", "/");
                    if (url.Contains("?"))
                        url += "&r=XCoder_v" + asm.CompileVersion;
                    else
                        url += "?r=XCoder_v" + asm.CompileVersion;
                    Process.Start(url);
                    e.Cancel = true;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            //webBrowser1.Navigate("http://www.newlifex.com/archiver/showforum-2.aspx", false);
            webBrowser1.Url = new Uri("http://www.newlifex.com/archiver/showforum-2.aspx?r=XCoder_v" + asm.CompileVersion);
            webBrowser1.Navigating += webBrowser1_Navigating;
        }
        #endregion

        #region 娣诲姞妯″瀷-@瀹佹尝-灏忚懀 2013
        private void 娣诲姞妯″瀷ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewModel.CreateForm().Show();
        }
        #endregion
    }
}