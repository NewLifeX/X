using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using NewLife.Reflection;

namespace XAgent
{
    /// <summary>服务主界面</summary>
    public partial class FrmMain : Form
    {
        #region 初始化
        /// <summary>初始化</summary>
        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            //this.Visible = false;

            AssemblyX ax = AssemblyX.Create(Assembly.GetEntryAssembly());
            String msg = String.Format("{0} {1} v{2}", ax.Title, ax.Name, ax.Version);
            this.Text = msg;
        }
        #endregion

        #region 托盘图标
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
        }

        private void 主界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            this.BringToFront();
        }

        private void 启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AssemblyX ax = AssemblyX.Create(Assembly.GetEntryAssembly());
            String msg = String.Format("{0} {1} v{2}", ax.Description, ax.Name, ax.Version);
            MessageBox.Show(msg);
        }
        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            //TODO: 刷服务状态
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            //this.Visible = false;
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Visible = false;
                e.Cancel = true;
            }
        }
    }
}
