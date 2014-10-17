using System;
using System.Collections;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using NewLife.Log;
using NewLife.Net;

namespace NewLife.Windows
{
    /// <summary>串口列表控件</summary>
    public partial class SerialPortList : UserControl
    {
        private SerialTransport _Port;
        /// <summary>端口</summary>
        public SerialTransport Port { get { return _Port; } set { _Port = value; } }

        /// <summary>选择的端口</summary>
        public String SelectedPort { get { return cbName.SelectedItem + ""; } set { } }

        #region 构造
        /// <summary></summary>
        public SerialPortList()
        {
            InitializeComponent();
        }

        private void SerialPortList_Load(object sender, EventArgs e)
        {
            LoadInfo();
        }
        #endregion

        #region 加载保存信息
        void LoadInfo()
        {
            ShowPorts();

            var ti = FindMenu("数据位");
            BindMenu(mi数据位, new Int32[] { 5, 6, 7, 8 }, On数据位Click);
            BindMenu(mi停止位, Enum.GetValues(typeof(StopBits)), On停止位Click);
            BindMenu(mi校验, Enum.GetValues(typeof(Parity)), On校验Click);

            cbBaundrate.DataSource = new Int32[] { 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 194000 };

            var cfg = SerialPortConfig.Current;
            cbName.SelectedItem = cfg.PortName;
            cbBaundrate.SelectedItem = cfg.BaudRate;
            SetMenuItem(mi数据位, cfg.DataBits);
            SetMenuItem(mi停止位, cfg.StopBits);
            SetMenuItem(mi校验, cfg.Parity);

            //cbEncoding.DataSource = new String[] { Encoding.Default.WebName, Encoding.ASCII.WebName, Encoding.UTF8.WebName };
            // 添加编码子菜单
            var encs = new Encoding[] { Encoding.Default, Encoding.ASCII, Encoding.UTF8 };
        }

        void SaveInfo()
        {
            try
            {
                var cfg = SerialPortConfig.Current;
                cfg.PortName = cbName.SelectedItem + "";
                cfg.BaudRate = (Int32)cbBaundrate.SelectedItem;
                //cfg.DataBits = (Int32)cbDataBit.SelectedItem;
                //cfg.StopBits = (StopBits)cbStopBit.SelectedItem;
                //cfg.Parity = (Parity)cbParity.SelectedItem;
                //cfg.Encoding = (Encoding)cbEncoding.SelectedItem;
                //cfg.WebEncoding = cbEncoding.SelectedItem + "";

                //cfg.HexSend = chkHEXSend.Checked;
                //cfg.HexShow = chkHEXShow.Checked;

                cfg.Save();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        String _ports = null;
        DateTime _nextport = DateTime.MinValue;
        /// <summary>下拉框显示串口</summary>
        public void ShowPorts()
        {
            if (_nextport > DateTime.Now) return;
            _nextport = DateTime.Now.AddSeconds(1);

            var ps = SerialTransport.GetPortNames();
            var str = String.Join(",", ps);
            // 如果端口有所改变，则重新绑定
            if (_ports != str)
            {
                _ports = str;
                var old = cbName.SelectedItem + "";
                cbName.DataSource = ps;
                if (!String.IsNullOrEmpty(old) && Array.IndexOf(ps, old) >= 0) cbName.SelectedItem = old;
            }
        }
        #endregion

        #region 菜单设置
        void On数据位Click(object sender, EventArgs e)
        {

        }
        void On停止位Click(object sender, EventArgs e)
        {

        }
        void On校验Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region 窗体
        //public new Boolean Focus() { return cbName.Focus(); }

        /// <summary>连接串口</summary>
        public void Connect()
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
            var cfg = SerialPortConfig.Current;

            if (Port == null)
                Port = new SerialTransport();
            else
                // 如果上次没有关闭，则关闭
                Port.Close();
            Port.PortName = name;
            Port.BaudRate = cfg.BaudRate;
            Port.Parity = cfg.Parity;
            Port.DataBits = cfg.DataBits;
            Port.StopBits = cfg.StopBits;
            Port.Open();
        }

        /// <summary>断开串口连接</summary>
        public void Disconnect()
        {
            if (Port != null) Port.Close();

            //this.Enabled = true;
            ShowPorts();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Enabled) ShowPorts();
        }
        #endregion

        #region 菜单辅助
        ToolStripItem FindMenu(String name)
        {
            return contextMenuStrip1.Items.Find(name, false)[0];
        }

        void BindMenu(ToolStripMenuItem ti, IEnumerable em, EventHandler handler)
        {
            foreach (var item in em)
            {
                var tsi = ti.DropDownItems.Add(item + "");
                tsi.Tag = item;
                tsi.Click += handler;
            }
        }

        void SetMenuItem(ToolStripMenuItem ti, Object value)
        {
            foreach (ToolStripMenuItem item in ti.DropDownItems)
            {
                item.Checked = item + "" == value + "";
            }
        }
        #endregion
    }
}
