using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Windows;
using XCoder;

namespace XNet
{
    [DisplayName("网络调试工具")]
    public partial class FrmMain : Form
    {
        NetServer _Server;
        ISocketClient _Client;
        static Task<NetServer[]> _task;

        #region 窗体
        static FrmMain()
        {
            _task = Task.Factory.StartNew(() => GetNetServers());
        }

        public FrmMain()
        {
            InitializeComponent();

            Icon = IcoHelper.GetIcon("网络");
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            txtReceive.UseWinFormControl();

            txtReceive.SetDefaultStyle(12);
            txtSend.SetDefaultStyle(12);
            numMutilSend.SetDefaultStyle(12);

            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            _task.ContinueWith(t =>
            {
                var dic = EnumHelper.GetDescriptions<WorkModes>();
                var list = dic.Select(kv => kv.Value).ToList();
                foreach (var item in t.Result)
                {
                    list.Add(item.Name);
                }
                this.Invoke(() =>
                {
                    cbMode.DataSource = list;

                    var cfg = NetConfig.Current;
                    if (cfg.Mode > 0 && dic.ContainsKey((WorkModes)cfg.Mode))
                        cbMode.SelectedItem = dic[(WorkModes)cfg.Mode];
                    else
                        cbMode.SelectedIndex = 0;
                });
            });

            // 加载保存的颜色
            UIConfig.Apply(txtReceive);

            LoadConfig();

            // 语音识别
            Task.Factory.StartNew(() =>
            {
                var sp = SpeechRecognition.Current;
                if (!sp.Enable) return;

                sp.Register("打开", () => this.Invoke(Connect))
                .Register("关闭", () => this.Invoke(Disconnect))
                .Register("退出", () => Application.Exit())
                .Register("发送", () => this.Invoke(() => btnSend_Click(null, null)));

                XTrace.WriteLine("语音识别前缀：{0} 可用命令：{1}", sp.Name, sp.GetAllKeys().Join());
            });
        }
        #endregion

        #region 加载/保存 配置
        void LoadConfig()
        {
            var cfg = NetConfig.Current;
            mi显示应用日志.Checked = cfg.ShowLog;
            mi显示网络日志.Checked = cfg.ShowSocketLog;
            mi显示接收字符串.Checked = cfg.ShowReceiveString;
            mi显示发送数据.Checked = cfg.ShowSend;
            mi显示接收数据.Checked = cfg.ShowReceive;
            mi显示统计信息.Checked = cfg.ShowStat;
            miHexSend.Checked = cfg.HexSend;

            txtSend.Text = cfg.SendContent;
            numMutilSend.Value = cfg.SendTimes;
            numSleep.Value = cfg.SendSleep;
            numThreads.Value = cfg.SendUsers;
            mi日志着色.Checked = cfg.ColorLog;

            cbLocal.DataSource = GetIPs();
            if (!cfg.Local.IsNullOrEmpty())
                cbLocal.SelectedItem = cfg.Local;
            else
                cbLocal.SelectedIndex = 0;

            // 历史地址列表
            if (!cfg.Address.IsNullOrEmpty()) cbRemote.DataSource = cfg.Address.Split(";");
            if (cfg.Port > 0) numPort.Value = cfg.Port;
        }

        void SaveConfig()
        {
            var cfg = NetConfig.Current;
            cfg.ShowLog = mi显示应用日志.Checked;
            cfg.ShowSocketLog = mi显示网络日志.Checked;
            cfg.ShowReceiveString = mi显示接收字符串.Checked;
            cfg.ShowSend = mi显示发送数据.Checked;
            cfg.ShowReceive = mi显示接收数据.Checked;
            cfg.ShowStat = mi显示统计信息.Checked;
            cfg.HexSend = miHexSend.Checked;

            cfg.SendContent = txtSend.Text;
            cfg.SendTimes = (Int32)numMutilSend.Value;
            cfg.SendSleep = (Int32)numSleep.Value;
            cfg.SendUsers = (Int32)numThreads.Value;
            cfg.ColorLog = mi日志着色.Checked;

            cfg.Local = cbLocal.Text;

            var addrs = (cfg.Address + "").Split(";").Distinct().ToList();
            if (!addrs.Contains(cbRemote.Text)) addrs.Insert(0, cbRemote.Text);
            while (addrs.Count > 10) addrs.RemoveAt(addrs.Count - 1);
            cfg.Address = addrs.Join(";");

            cfg.Port = (Int32)numPort.Value;

            cfg.Save();
        }
        #endregion

        #region 收发数据
        void Connect()
        {
            _Server = null;
            _Client = null;

            var mode = GetMode();
            var local = cbLocal.Text;
            var remote = cbRemote.Text;
            var port = (Int32)numPort.Value;

            var cfg = NetConfig.Current;
            cfg.Mode = (Byte)mode;

            switch (mode)
            {
                case WorkModes.UDP_TCP:
                    _Server = new NetServer();
                    break;
                case WorkModes.UDP_Server:
                    _Server = new NetServer();
                    _Server.ProtocolType = NetType.Udp;
                    break;
                case WorkModes.TCP_Server:
                    _Server = new NetServer();
                    _Server.ProtocolType = NetType.Tcp;
                    break;
                case WorkModes.TCP_Client:
                    _Client = new TcpSession();
                    break;
                case WorkModes.UDP_Client:
                    _Client = new UdpServer();
                    break;
                default:
                    if ((Int32)mode > 0)
                    {
                        var ns = GetNetServers().Where(n => n.Name == cbMode.Text).FirstOrDefault();
                        if (ns == null) throw new XException("未识别服务[{0}]", mode);

                        _Server = ns.GetType().CreateInstance() as NetServer;
                    }
                    break;
            }

            if (_Client != null)
            {
                _Client.Log = cfg.ShowLog ? XTrace.Log : Logger.Null;
                if (!local.Contains("所有本地")) _Client.Local.Host = local;
                _Client.Received += OnReceived;
                _Client.Remote.Port = port;
                _Client.Remote.Host = remote;

                _Client.LogSend = cfg.ShowSend;
                _Client.LogReceive = cfg.ShowReceive;

                _Client.Open();

                "已连接服务器".SpeechTip();
            }
            else if (_Server != null)
            {
                if (_Server == null) _Server = new NetServer();
                _Server.Log = cfg.ShowLog ? XTrace.Log : Logger.Null;
                _Server.SocketLog = cfg.ShowSocketLog ? XTrace.Log : Logger.Null;
                _Server.Port = port;
                if (!local.Contains("所有本地")) _Server.Local.Host = local;
                _Server.Received += OnReceived;

                _Server.LogSend = cfg.ShowSend;
                _Server.LogReceive = cfg.ShowReceive;

                // 加大会话超时时间到1天
                _Server.SessionTimeout = 24 * 3600;

                _Server.Start();

                "正在监听{0}".F(port).SpeechTip();
            }

            pnlSetting.Enabled = false;
            btnConnect.Text = "关闭";

            cfg.Save();

            _timer = new TimerX(ShowStat, null, 5000, 5000);

            BizLog = TextFileLog.Create("NetLog");
        }

        void Disconnect()
        {
            if (_Client != null)
            {
                _Client.Dispose();
                _Client = null;

                "关闭连接".SpeechTip();
            }
            if (_Server != null)
            {
                "停止监听{0}".F(_Server.Port).SpeechTip();
                _Server.Dispose();
                _Server = null;
            }
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            pnlSetting.Enabled = true;
            btnConnect.Text = "打开";
        }

        TimerX _timer;
        String _lastStat;
        void ShowStat(Object state)
        {
            if (!NetConfig.Current.ShowStat) return;

            var msg = "";
            if (_Client != null)
                msg = _Client.GetStat();
            else if (_Server != null)
                msg = _Server.GetStat();

            if (!msg.IsNullOrEmpty() && msg != _lastStat)
            {
                _lastStat = msg;
                XTrace.WriteLine(msg);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            SaveConfig();

            var btn = sender as Button;
            if (btn.Text == "打开")
                Connect();
            else
                Disconnect();
        }

        /// <summary>业务日志输出</summary>
        ILog BizLog;

        void OnReceived(Object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;
            if (session == null)
            {
                var ns = sender as INetSession;
                if (ns == null) return;
                session = ns.Session;
            }

            if (NetConfig.Current.ShowReceiveString)
            {
                var line = e.ToStr();
                XTrace.WriteLine(line);

                if (BizLog != null) BizLog.Info(line);
            }
        }

        Int32 _pColor = 0;
        Int32 BytesOfReceived = 0;
        Int32 BytesOfSent = 0;
        Int32 lastReceive = 0;
        Int32 lastSend = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            //if (!pnlSetting.Enabled)
            {
                var rcount = BytesOfReceived;
                var tcount = BytesOfSent;
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

                var cfg = NetConfig.Current;
                if (cfg.ColorLog) txtReceive.ColourDefault(_pColor);
                _pColor = txtReceive.TextLength;
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var str = txtSend.Text;
            if (String.IsNullOrEmpty(str))
            {
                MessageBox.Show("发送内容不能为空！", Text);
                txtSend.Focus();
                return;
            }

            // 多次发送
            var count = (Int32)numMutilSend.Value;
            var sleep = (Int32)numSleep.Value;
            var ths = (Int32)numThreads.Value;
            if (count <= 0) count = 1;
            if (sleep <= 0) sleep = 1;

            SaveConfig();

            //if (_Client == null)
            //{
            //    XTrace.WriteLine("未连接服务端！");
            //    return;
            //}

            var cfg = NetConfig.Current;

            // 处理换行
            str = str.Replace("\n", "\r\n");
            var buf = cfg.HexSend ? str.ToHex() : str.GetBytes();

            if (_Client != null)
            {
                if (ths <= 1)
                {
                    _Client.SendMulti(buf, count, sleep);
                }
                else
                {
                    // 多线程测试
                    //Task.Factory.StartNew(() =>
                    //{
                    //    for (int i = 0; i < ths; i++)
                    //    {
                    //        var client = _Client.Remote.CreateRemote();
                    //        client.StatSend = _Client.StatSend;
                    //        client.StatReceive = _Client.StatReceive;
                    //        //client.SendAsync(buf, count, sleep).ContinueWith(t => client.Dispose());
                    //        client.SendAsync(buf, count, sleep);
                    //    }
                    //}).LogException();
                    Parallel.For(0, ths, n =>
                    {
                        var client = _Client.Remote.CreateRemote();
                        client.StatSend = _Client.StatSend;
                        client.StatReceive = _Client.StatReceive;
                        //client.SendAsync(buf, count, sleep).ContinueWith(t => client.Dispose());
                        client.SendMulti(buf, count, sleep);
                    });
                }
            }
            else if (_Server != null)
            {
                Task.Run(async () =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        var cs = await _Server.SendAllAsync(buf);
                        XTrace.WriteLine("已向[{0}]个客户端发送[{1}]数据", cs, buf.Length);
                        if (sleep > 0) await Task.Delay(sleep);
                    }
                });
            }
        }
        #endregion

        #region 右键菜单
        private void mi清空_Click(object sender, EventArgs e)
        {
            txtReceive.Clear();
            BytesOfReceived = 0;
        }

        private void mi清空2_Click(object sender, EventArgs e)
        {
            txtSend.Clear();
            BytesOfSent = 0;
        }

        private void mi显示应用日志_Click(object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示网络日志_Click(object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示发送数据_Click(object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示接收数据_Click(object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示统计信息_Click(object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            NetConfig.Current.ShowStat = mi.Checked = !mi.Checked;
        }

        private void mi显示接收字符串_Click(object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            NetConfig.Current.ShowReceiveString = mi.Checked = !mi.Checked;
        }

        private void miHex发送_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            NetConfig.Current.HexSend = mi.Checked = !mi.Checked;
        }

        private void 查看Tcp参数ToolStripMenuItem_Click(Object sender, EventArgs e)
        {
            NetHelper.ShowTcpParameters();
        }

        private void 设置最大TcpToolStripMenuItem_Click(Object sender, EventArgs e)
        {
            NetHelper.SetTcpMax();
        }

        private void mi日志着色_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }
        #endregion

        private void cbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mode = GetMode();
            if ((Int32)mode == 0) return;

            switch (mode)
            {
                case WorkModes.TCP_Client:
                case WorkModes.UDP_Client:
                    //cbRemote.DropDownStyle = ComboBoxStyle.DropDown;
                    //cbRemote.DataSource = null;
                    //cbRemote.Items.Clear();
                    //cbRemote.Text = NetConfig.Current.Address;
                    break;
                default:
                case WorkModes.UDP_TCP:
                case WorkModes.UDP_Server:
                case WorkModes.TCP_Server:
                    //cbLocal.DropDownStyle = ComboBoxStyle.DropDownList;
                    //cbLocal.DataSource = GetIPs();
                    break;
                case (WorkModes)0xFF:
                    //cbLocal.DropDownStyle = ComboBoxStyle.DropDownList;
                    //cbLocal.DataSource = GetIPs();

                    // 端口
                    var ns = GetNetServers().Where(n => n.Name == cbMode.Text).FirstOrDefault();
                    if (ns != null && ns.Port > 0) numPort.Value = ns.Port;

                    break;
            }
        }

        WorkModes GetMode()
        {
            var mode = cbMode.Text;
            if (String.IsNullOrEmpty(mode)) return (WorkModes)0;

            var list = EnumHelper.GetDescriptions<WorkModes>().Where(kv => kv.Value == mode).ToList();
            if (list.Count == 0) return (WorkModes)0xFF;

            return (WorkModes)list[0].Key;
        }

        static String[] GetIPs()
        {
            var list = NetHelper.GetIPs().Select(e => e.ToString()).ToList();
            list.Insert(0, "所有本地IPv4/IPv6");
            list.Insert(1, IPAddress.Any.ToString());
            list.Insert(2, IPAddress.IPv6Any.ToString());

            return list.ToArray();
        }

        static NetServer[] _ns;
        static NetServer[] GetNetServers()
        {
            if (_ns != null) return _ns;

            lock (typeof(FrmMain))
            {
                if (_ns != null) return _ns;

                var list = new List<NetServer>();
                foreach (var item in typeof(NetServer).GetAllSubclasses(true))
                {
                    try
                    {
                        var ns = item.CreateInstance() as NetServer;
                        if (ns != null) list.Add(ns);
                    }
                    catch { }
                }

                return _ns = list.ToArray();
            }
        }
    }
}