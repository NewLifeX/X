using System;
using System.Threading;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using XCoder;

namespace XCom
{
    public partial class FrmMain : Form
    {
        #region 窗体
        public FrmMain()
        {
            InitializeComponent();

            //var asmx = AssemblyX.Entry;
            //this.Text = asmx.Title;

            this.Icon = IcoHelper.GetIcon("串口");
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            spList.ReceivedString += OnReceived;

            var menu = spList.Menu;
            txtReceive.ContextMenuStrip = menu;

            // 添加清空
            menu.Items.Insert(0, new ToolStripSeparator());
            //var ti = menu.Items.Add("清空");
            var ti = new ToolStripMenuItem("清空");
            menu.Items.Insert(0, ti);
            ti.Click += mi清空_Click;

            ti = new ToolStripMenuItem("字体");
            menu.Items.Add(ti);
            ti.Click += mi字体_Click;

            ti = new ToolStripMenuItem("前景色");
            menu.Items.Add(ti);
            ti.Click += mi前景色_Click;

            ti = new ToolStripMenuItem("背景色");
            menu.Items.Add(ti);
            ti.Click += mi背景色_Click;

            // 加载保存的颜色
            var ui = UIConfig.Current;
            if (ui != null)
            {
                try
                {
                    txtReceive.Font = ui.Font;
                    txtReceive.BackColor = ui.BackColor;
                    txtReceive.ForeColor = ui.ForeColor;
                }
                catch
                {
                    ui.Font = txtReceive.Font;
                    ui.BackColor = txtReceive.BackColor;
                    ui.ForeColor = txtReceive.ForeColor;
                }
            }
        }
        #endregion

        #region 收发数据
        void Connect()
        {
            spList.Connect();
            var st = spList.Port;
            st.FrameSize = 8;

            // 需要考虑UI线程
            st.Disconnected += (s, e) => this.Invoke(Disconnect);

            btnConnect.Text = "关闭";
        }

        void Disconnect()
        {
            spList.Port.Disconnected -= (s, e) => this.Invoke(Disconnect);
            spList.Disconnect();

            btnConnect.Text = "打开";
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn.Text == "打开")
                Connect();
            else
                Disconnect();
        }

        void OnReceived(Object sender, EventArgs<String> e)
        {
            var line = e.Arg;
            //XTrace.UseWinFormWriteLog(txtReceive, line, 100000);
            TextControlLog.WriteLog(txtReceive, line);
        }

        Int32 lastReceive = 0;
        Int32 lastSend = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            var sp = spList.Port;
            if (sp != null)
            {
                // 检查串口是否已断开，自动关闭已断开的串口，避免内存暴涨
                if (!spList.Enabled && btnConnect.Text == "打开")
                {
                    Disconnect();
                    return;
                }

                var rcount = spList.BytesOfReceived;
                var tcount = spList.BytesOfSent;
                if (rcount != lastReceive)
                {
                    gbReceive.Text = (gbReceive.Tag + "").Replace("0", rcount + "");
                    lastReceive = rcount;
                }
                if (tcount != lastSend)
                {
                    gbSend.Text = (gbSend.Tag + "").Replace("0", tcount + "");
                    lastSend = tcount;
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var str = txtSend.Text;
            if (String.IsNullOrEmpty(str))
            {
                MessageBox.Show("发送内容不能为空！", this.Text);
                txtSend.Focus();
                return;
            }

            // 多次发送
            var count = (Int32)numMutilSend.Value;
            for (int i = 0; i < count; i++)
            {
                spList.Send(str);

                Thread.Sleep(100);
            }
        }
        #endregion

        #region 右键菜单
        private void mi清空_Click(object sender, EventArgs e)
        {
            txtReceive.Clear();
            spList.ClearReceive();
        }

        private void mi清空2_Click(object sender, EventArgs e)
        {
            txtSend.Clear();
            spList.ClearSend();
        }

        void mi字体_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = txtReceive.Font;
            if (fontDialog1.ShowDialog() != DialogResult.OK) return;

            txtReceive.Font = fontDialog1.Font;

            var ui = UIConfig.Current;
            ui.Font = txtReceive.Font;
            ui.Save();
        }

        void mi前景色_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = txtReceive.ForeColor;
            if (colorDialog1.ShowDialog() != DialogResult.OK) return;

            txtReceive.ForeColor = colorDialog1.Color;

            var ui = UIConfig.Current;
            ui.ForeColor = txtReceive.ForeColor;
            ui.Save();
        }

        void mi背景色_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = txtReceive.BackColor;
            if (colorDialog1.ShowDialog() != DialogResult.OK) return;

            txtReceive.BackColor = colorDialog1.Color;

            var ui = UIConfig.Current;
            ui.BackColor = txtReceive.BackColor;
            ui.Save();
        }
        #endregion
    }
}