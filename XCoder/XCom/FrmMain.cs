using System;
using System.Drawing;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using NewLife.IO;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;

namespace XCom
{
    public partial class FrmMain : Form
    {
        Com _Com;

        public FrmMain()
        {
            InitializeComponent();

            var asmx = AssemblyX.Entry;
            this.Text = asmx.Title;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            LoadInfo();

            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            var ms = FileSource.GetFileResource(Assembly.GetExecutingAssembly(), "leaf.ico");
            this.Icon = new Icon(ms);
        }

        void LoadInfo()
        {
            //cbName.DataSource = SerialPort.GetPortNames();
            //cbName.DataSource = IOHelper.GetPortNames();
            ShowPorts();
            cbStopBit.DataSource = Enum.GetValues(typeof(StopBits));
            cbParity.DataSource = Enum.GetValues(typeof(Parity));

            cbStopBit.SelectedItem = StopBits.One;

            cbBaundrate.DataSource = new Int32[] { 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 194000 };
            //cbBaundrate.SelectedItem = 115200;

            cbDataBit.DataSource = new Int32[] { 5, 6, 7, 8 };
            //cbDataBit.SelectedItem = 8;

            cbEncoding.DataSource = new String[] { Encoding.Default.WebName, Encoding.ASCII.WebName, Encoding.UTF8.WebName };
            //cbEncoding.SelectedItem = Encoding.Default.WebName;

            var cfg = Config.Current;
            cbName.SelectedItem = cfg.PortName;
            cbBaundrate.SelectedItem = cfg.BaudRate;
            cbDataBit.SelectedItem = cfg.DataBits;
            cbStopBit.SelectedItem = cfg.StopBits;
            cbParity.SelectedItem = cfg.Parity;
            cbEncoding.SelectedItem = cfg.WebEncoding;

            chkHEXShow.Checked = cfg.HexShow;
            chkHEXSend.Checked = cfg.HexSend;
        }

        void SaveInfo()
        {
            try
            {
                var cfg = Config.Current;
                cfg.PortName = cbName.SelectedItem + "";
                cfg.BaudRate = (Int32)cbBaundrate.SelectedItem;
                cfg.DataBits = (Int32)cbDataBit.SelectedItem;
                cfg.StopBits = (StopBits)cbStopBit.SelectedItem;
                cfg.Parity = (Parity)cbParity.SelectedItem;
                //cfg.Encoding = (Encoding)cbEncoding.SelectedItem;
                cfg.WebEncoding = cbEncoding.SelectedItem + "";

                cfg.HexSend = chkHEXSend.Checked;
                cfg.HexShow = chkHEXShow.Checked;

                cfg.Save();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        String _ports = null;
        DateTime _nextport = DateTime.MinValue;
        void ShowPorts()
        {
            if (_nextport > DateTime.Now) return;
            _nextport = DateTime.Now.AddSeconds(1);

            var ps = IOHelper.GetPortNames();
            var str = String.Join(",", ps);
            if (_ports != str)
            {
                _ports = str;
                cbName.DataSource = ps;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn.Text == "打开串口")
            {
                var name = cbName.SelectedItem + "";
                if (String.IsNullOrEmpty(name))
                {
                    MessageBox.Show("请选择串口！", this.Text);
                    cbName.Focus();
                    return;
                }
                var p = name.IndexOf("(");
                if (p > 0) name = name.Substring(0, p);

                SaveInfo();
                var cfg = Config.Current;

                // 如果上次没有关闭，则关闭
                if (_Com != null) _Com.Dispose();

                var sp = new SerialPort(name, cfg.BaudRate, cfg.Parity, cfg.DataBits, cfg.StopBits);
                sp.Open();
                sp.DtrEnable = chkDTR.Checked;
                sp.BreakState = chkBreak.Checked;
                sp.RtsEnable = chkRTS.Checked;

                _Com = new Com { Serial = sp };

                // 计算编码
                //if (!String.IsNullOrEmpty(cfg.Encoding) && !cfg.Encoding.EqualIgnoreCase("Default")) _Com.Encoding = Encoding.GetEncoding(cfg.Encoding);
                _Com.Encoding = cfg.Encoding;

                _Com.Received += _Com_Received;
                _Com.Listen();

                gbSet.Enabled = false;
                gbSet2.Enabled = false;
                //gbSet3.Enabled = true;
                //timer1.Enabled = true;
                btn.Text = "关闭串口";
            }
            else
            {
                if (_Com != null)
                {
                    _Com.Dispose();
                    _Com = null;
                }

                gbSet.Enabled = true;
                gbSet2.Enabled = true;
                //gbSet3.Enabled = false;
                //timer1.Enabled = false;
                btn.Text = "打开串口";

                ShowPorts();
            }
        }

        void _Com_Received(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null || e.Data.Length < 1) return;

            var line = "";
            if (chkHEXShow.Checked)
                line = e.Data.ToHex();
            else
                line = _Com.Encoding.GetString(e.Data);

            XTrace.UseWinFormWriteLog(txtReceive, line, 100000);
        }

        Int32 lastReceive = 0;
        Int32 lastSend = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_Com != null)
            {
                if (_Com.BytesOfReceived != lastReceive)
                {
                    gbReceive.Text = (gbReceive.Tag + "").Replace("0", _Com.BytesOfReceived + "");
                    lastReceive = _Com.BytesOfReceived;
                }
                if (_Com.BytesOfSent != lastSend)
                {
                    gbSend.Text = (gbSend.Tag + "").Replace("0", _Com.BytesOfSent + "");
                    lastSend = _Com.BytesOfSent;
                }
            }
            else
            {
                ShowPorts();
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

            // 16进制发送
            Byte[] data = null;
            if (chkHEXSend.Checked)
                data = DataHelper.FromHex(str);
            else
                data = _Com.Encoding.GetBytes(str);

            // 多次发送
            var count = 1;
            if (chkMutilSend.Checked) count = (Int32)numMutilSend.Value;

            for (int i = 0; i < count; i++)
            {
                _Com.Write(data);
            }
        }

        private void btnClearSend_Click(object sender, EventArgs e)
        {
            if (_Com != null && _Com.Serial != null) _Com.Serial.DiscardOutBuffer();
            txtSend.Clear();
        }

        private void btnSendReceive_Click(object sender, EventArgs e)
        {
            if (_Com != null && _Com.Serial != null) _Com.Serial.DiscardInBuffer();
            txtReceive.Clear();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (_Com != null)
            {
                _Com.BytesOfReceived = 0;
                _Com.BytesOfSent = 0;
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_Com != null) _Com.Dispose();
        }

        private void cbEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_Com != null) _Com.Encoding = Encoding.GetEncoding(cbEncoding.SelectedItem + "");
        }
    }
}