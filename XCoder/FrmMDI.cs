using System;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Reflection;
using NewLife.Net;
using System.IO;
using NewLife.Log;
using NewLife.Threading;

namespace XCoder
{
    public partial class FrmMDI : Form
    {
        #region 窗口初始化
        Task<Type[]> _load;

        public FrmMDI()
        {
            _load = Task<Type[]>.Factory.StartNew(() => typeof(IXForm).GetAllSubclasses(true).ToArray());

            InitializeComponent();

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();

            Icon = Source.GetIcon();
        }

        private void FrmMDI_Shown(Object sender, EventArgs e)
        {
            var set = XConfig.Current;
            if (set.Width > 0 || set.Height > 0)
            {
                Width = set.Width;
                Height = set.Height;
                Top = set.Top;
                Left = set.Left;
            }

            var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            if (set.Title.IsNullOrEmpty()) set.Title = asm.Title;
            Text = String.Format("{2} v{0} {1:HH:mm:ss}", asm.CompileVersion, asm.Compile, set.Title);

            _load.ContinueWith(t => LoadForms(t.Result));

            ThreadPoolX.QueueUserWorkItem(() => CheckUpdate(true));
        }

        void LoadForms(Type[] ts)
        {
            var name = XConfig.Current.LastTool + "";
            foreach (var item in ts)
            {
                if (item.FullName.EqualIgnoreCase(name))
                {
                    this.Invoke(() => CreateForm(item.CreateInstance() as Form));

                    break;
                }
            }

            this.Invoke(() =>
            {
                var root = toolsMenu;
                foreach (var item in ts)
                {
                    var mi = root.DropDownItems.Add(item.GetDisplayName() ?? item.FullName);
                    mi.Tag = item;
                    mi.Click += (s, e) =>
                    {
                        var tsi = s as ToolStripItem;
                        var type = tsi.Tag as Type;
                        CreateForm(type.CreateInstance() as Form);
                    };
                }
            });
        }

        private void FrmMDI_FormClosing(Object sender, FormClosingEventArgs e)
        {
            var set = XConfig.Current;
            var area = Screen.PrimaryScreen.WorkingArea;
            if (Left >= 0 && Top >= 0 && Width < area.Width - 60 && Height < area.Height - 60)
            {
                set.Width = Width;
                set.Height = Height;
                set.Top = Top;
                set.Left = Left;
                set.Save();
            }
        }
        #endregion

        #region 应用窗口
        void CreateForm<TForm>() where TForm : Form, new()
        {
            var name = typeof(TForm).FullName;
            var cfg = XConfig.Current;
            if (name != cfg.LastTool)
            {
                cfg.LastTool = name;
                cfg.Save();
            }

            var frm = new TForm();
            CreateForm(frm);
        }

        void CreateForm(Form frm)
        {
            var name = frm.GetType().FullName;
            var cfg = XConfig.Current;
            if (name != cfg.LastTool)
            {
                cfg.LastTool = name;
                cfg.Save();
            }

            frm.MdiParent = this;
            frm.WindowState = FormWindowState.Maximized;
            frm.Show();
        }
        #endregion

        #region 菜单控制
        private void ShowNewForm(Object sender, EventArgs e) { }

        private void CascadeToolStripMenuItem_Click(Object sender, EventArgs e) { LayoutMdi(MdiLayout.Cascade); }

        private void TileVerticalToolStripMenuItem_Click(Object sender, EventArgs e) { LayoutMdi(MdiLayout.TileVertical); }

        private void TileHorizontalToolStripMenuItem_Click(Object sender, EventArgs e) { LayoutMdi(MdiLayout.TileHorizontal); }

        private void ArrangeIconsToolStripMenuItem_Click(Object sender, EventArgs e) { LayoutMdi(MdiLayout.ArrangeIcons); }

        private void CloseAllToolStripMenuItem_Click(Object sender, EventArgs e)
        {
            foreach (var childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void aboutToolStripMenuItem_Click(Object sender, EventArgs e)
        {
            Process.Start("http://www.NewLifeX.com");
        }
        #endregion

        #region 自动更新
        private void 检查更新ToolStripMenuItem_Click(Object sender, EventArgs e)
        {
            ThreadPoolX.QueueUserWorkItem(() => CheckUpdate(false));
        }

        private void CheckUpdate(Boolean auto)
        {
            if (auto) XTrace.WriteLine("自动更新！");

            Upgrade.DeleteBuckup();

            var cfg = XConfig.Current;
            if (cfg.LastUpdate.Date < DateTime.Now.Date || !auto)
            {
                cfg.LastUpdate = DateTime.Now;
                cfg.Save();

                var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var up = new Upgrade();
                up.Log = XTrace.Log;
                up.Name = "XCoder";
                up.Server = cfg.UpdateServer;
                up.UpdatePath = root.CombinePath(up.UpdatePath);
                if (up.Check())
                {
                    up.Download();
                    if (!auto || MessageBox.Show("发现新版本{0}，是否更新？".F(up.Links[0].Time), "自动更新", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        up.Update();
                }
                else if (!auto)
                {
                    if (up.Links != null && up.Links.Length > 0)
                        MessageBox.Show("没有可用更新！最新{0}".F(up.Links[0].Time), "自动更新");
                    else
                        MessageBox.Show("没有可用更新！", "自动更新");
                }
            }
        }
        #endregion
    }
}