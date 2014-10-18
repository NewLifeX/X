using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NewLife.Log;
using NewLife.Net;
using NewLife.Threading;
using XCoder;
using NewLife;

namespace XCom
{
    public partial class FrmMain : Form
    {
        SerialTransport _Com;
        Int32 BytesOfReceived;
        Int32 BytesOfSent;

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
            LoadInfo();

            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            var menu = spList.Menu;
            txtReceive.ContextMenuStrip = menu;

            // 添加清空
            var ti = menu.Items.Add("清空");
            ti.Click += mi清空_Click;
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_Com != null) _Com.Dispose();
            SaveInfo();
        }
        #endregion

        #region 加载保存信息
        void LoadInfo()
        {
            var cfg = SerialPortConfig.Current;

            // 发送菜单HEX编码
            miHEX编码2.Checked = cfg.HexSend;
        }

        void SaveInfo()
        {
            try
            {
                var cfg = SerialPortConfig.Current;

                cfg.HexSend = miHEX编码2.Checked;

                cfg.Save();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
        #endregion

        #region 收发数据
        void Connect()
        {
            //var name = cbName.SelectedItem + "";
            //if (String.IsNullOrEmpty(name))
            //{
            //    MessageBox.Show("请选择串口！", this.Text);
            //    cbName.Focus();
            //    return;
            //}
            //var p = name.IndexOf("(");
            //if (p > 0) name = name.Substring(0, p);

            //SaveInfo();
            var cfg = SerialPortConfig.Current;

            //// 如果上次没有关闭，则关闭
            //if (_Com != null) _Com.Dispose();

            var name = "";
            var sp = new SerialPort(name, cfg.BaudRate, cfg.Parity, cfg.DataBits, cfg.StopBits);
            var st = new SerialTransport { Serial = sp };
            st.FrameSize = 8;
            st.Open();
            //_Com.Disconnected += (s, e) => Disconnect();
            // 需要考虑UI线程
            st.Disconnected += (s, e) => this.Invoke(Disconnect);

            //sp.DtrEnable = chkDTR.Checked;
            //sp.RtsEnable = chkRTS.Checked;
            //if (chkBreak.Checked) sp.BreakState = chkBreak.Checked;

            st.Received += _Com_Received;
            st.ReceiveAsync();

            spList.Enabled = false;
            btnConnect.Text = "关闭";

            // 必须完成串口打开以后再赋值，否则定时器会轮询导致报错
            _Com = st;
        }

        void Disconnect()
        {
            var cm = _Com;
            if (cm != null)
            {
                _Com = null;
                //cm.Dispose();
                // 异步调用释放，避免死锁卡死界面UI
                ThreadPoolX.QueueUserWorkItem(() => cm.Dispose());
            }

            spList.Enabled = true;
            btnConnect.Text = "打开";

            spList.ShowPorts();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn.Text == "打开")
                Connect();
            else
                Disconnect();
        }

        MemoryStream _stream;
        StreamReader _reader;
        Byte[] _Com_Received(ITransport sender, Byte[] data)
        {
            if (data == null || data.Length < 1) return null;

            BytesOfReceived += data.Length;

            var cfg = SerialPortConfig.Current;

            var line = "";
            if (cfg.HexShow)
            {
                line = data.ToHex();
                if (cfg.HexNewLine) line += Environment.NewLine;
            }
            else
            {
                line = cfg.Encoding.GetString(data);
                if (_stream == null)
                    _stream = new MemoryStream();
                else if (_stream.Length > 10 * 1024 && _stream.Position == _stream.Length) // 达到最大大小时，从头开始使用
                    _stream = new MemoryStream();
                _stream.Write(data);
                _stream.Seek(-1 * data.Length, SeekOrigin.Current);

                if (_reader == null ||
                    _reader.BaseStream != _stream ||
                    _reader.CurrentEncoding != cfg.Encoding) _reader = new StreamReader(_stream, cfg.Encoding);
                line = _reader.ReadToEnd();
            }

            //XTrace.UseWinFormWriteLog(txtReceive, line, 100000);
            TextControlLog.WriteLog(txtReceive, line);

            return null;
        }

        Int32 lastReceive = 0;
        Int32 lastSend = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            var sp = _Com;
            if (sp != null)
            {
                if ((sp.Serial == null || !sp.Serial.IsOpen) && btnConnect.Text == "打开")
                {
                    Disconnect();
                    return;
                }

                if (BytesOfReceived != lastReceive)
                {
                    gbReceive.Text = (gbReceive.Tag + "").Replace("0", BytesOfReceived + "");
                    lastReceive = BytesOfReceived;
                }
                if (BytesOfSent != lastSend)
                {
                    gbSend.Text = (gbSend.Tag + "").Replace("0", BytesOfSent + "");
                    lastSend = BytesOfSent;
                }

                // 检查串口是否已断开，自动关闭已断开的串口，避免内存暴涨
                if (sp.Serial != null && !sp.Serial.IsOpen) btnConnect_Click(btnConnect, EventArgs.Empty);
            }
            else
            {
                spList.ShowPorts();
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

            var cfg = SerialPortConfig.Current;
            // 16进制发送
            Byte[] data = null;
            if (cfg.HexSend)
                data = str.ToHex();
            else
                data = cfg.Encoding.GetBytes(str);
            if (data != null)
            {
                // 多次发送
                var count = (Int32)numMutilSend.Value;
                for (int i = 0; i < count; i++)
                {
                    BytesOfSent += data.Length;

                    _Com.Send(data);
                    Thread.Sleep(100);
                }
            }
        }
        #endregion

        #region 接收右键菜单
        private void mi清空_Click(object sender, EventArgs e)
        {
            txtReceive.Clear();
            var sp = _Com;
            if (sp != null)
            {
                BytesOfReceived = 0;
                if (sp.Serial != null) sp.Serial.DiscardInBuffer();
            }
        }
        #endregion

        #region 发送右键菜单
        private void mi清空2_Click(object sender, EventArgs e)
        {
            txtSend.Clear();
            if (_Com != null)
            {
                BytesOfSent = 0;
                _Com.Serial.DiscardOutBuffer();
            }
        }

        private void miHEX编码2_Click(object sender, EventArgs e)
        {
            SerialPortConfig.Current.HexSend = miHEX编码2.Checked;
        }
        #endregion
    }
}