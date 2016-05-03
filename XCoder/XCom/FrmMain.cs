using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Log;
using NewLife.Threading;
using NewLife.Windows;
using XCoder;

namespace XCom
{
    [DisplayName("串口调试工具")]
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
            txtReceive.SetDefaultStyle(12);
            txtSend.SetDefaultStyle(12);
            numMutilSend.SetDefaultStyle(12);

            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            //spList.ReceivedString += OnReceived;

            var menu = spList.Menu;
            txtReceive.ContextMenuStrip = menu;

            // 添加清空
            menu.Items.Insert(0, new ToolStripSeparator());
            //var ti = menu.Items.Add("清空");
            var ti = new ToolStripMenuItem("清空");
            menu.Items.Insert(0, ti);
            ti.Click += mi清空_Click;

            // 加载保存的颜色
            UIConfig.Apply(txtReceive);
        }
        #endregion

        #region 收发数据
        void Connect()
        {
            spList.Connect();
            var st = spList.Port;
            //st.FrameSize = 8;
            if (st == null) return;

            // 需要考虑UI线程
            st.Disconnected += (s, e) => this.Invoke(Disconnect);

            // 发现USB2401端口，自动发送设置命令
            if (st.Description.Contains("USB2401") || st.Description.Contains("USBSER"))
            {
                var cmd = "AT+SET=00070000000000";
                st.Send(cmd.GetBytes());
                //XTrace.WriteLine(cmd);
                TextControlLog.WriteLog(txtReceive, cmd);
            }

            "连接串口{0}".F(st.PortName).SpeechTip();

            btnConnect.Text = "关闭";

            BizLog = TextFileLog.Create("SerialLog");
        }

        void Disconnect()
        {
            var st = spList.Port;
            if (st != null) st.Disconnected -= (s, e) => this.Invoke(Disconnect);
            spList.Disconnect();

            "串口已断开".SpeechTip();

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

        /// <summary>业务日志输出</summary>
        ILog BizLog;

        void OnReceived(Object sender, StringEventArgs e)
        {
            var line = e.Value;
            //XTrace.UseWinFormWriteLog(txtReceive, line, 100000);
            TextControlLog.WriteLog(txtReceive, line);

            if (BizLog != null) BizLog.Info(line);
        }

        Int32 _pColor = 0;
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

                //ChangeColor();
                if (cbColor.Checked) txtReceive.ColourDefault(_pColor);
                _pColor = txtReceive.TextLength;
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
            var sleep = (Int32)numSleep.Value;
            if (count <= 0) count = 1;
            if (sleep <= 0) sleep = 100;

            // 处理换行
            str = str.Replace("\n", "\r\n");

            if (count == 1)
            {
                spList.Send(str);
                return;
            }

            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    spList.Send(str);

                    if (count > 1) Thread.Sleep(sleep);
                }
            }).LogException();
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