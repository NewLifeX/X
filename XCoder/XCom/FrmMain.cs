using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using NewLife.Threading;
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

            var menu = spList.Menu;
            txtReceive.ContextMenuStrip = menu;

            // 添加清空
            var ti = menu.Items.Add("清空");
            ti.Click += mi清空_Click;
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
            spList.Received += OnReceived;

            btnConnect.Text = "关闭";
        }

        void Disconnect()
        {
            spList.Port.Disconnected -= (s, e) => this.Invoke(Disconnect);
            spList.Received -= OnReceived;
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
        #endregion
    }
}