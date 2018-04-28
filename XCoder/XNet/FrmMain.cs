using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Windows;
using XCoder;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace XNet
{
    [DisplayName("网络调试工具")]
    public partial class FrmMain : Form, IXForm
    {
        NetServer _Server;
        ISocketClient _Client;
        static Task<Dictionary<String, Type>> _task;

        /// <summary>业务日志输出</summary>
        ILog BizLog;

        #region 窗体
        static FrmMain()
        {
            _task = Task.Factory.StartNew(() => GetNetServers());
        }

        public FrmMain()
        {
            InitializeComponent();

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();

            Icon = IcoHelper.GetIcon("网络");
        }

        private void FrmMain_Load(Object sender, EventArgs e)
        {
            var log = TextFileLog.Create(null, "Net_{0:yyyy_MM_dd}.log");
            BizLog = txtReceive.Combine(log);
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
                //var ds = dic.ToDictionary(s => s.Value, s => s.Value);
                foreach (var item in t.Result)
                {
                    list.Add(item.Key);
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

                BizLog.Info("语音识别前缀：{0} 可用命令：{1}", sp.Name, sp.GetAllKeys().Join());
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
            cfg.AddAddress(cbRemote.Text);

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
                    if (mode > 0)
                    {
                        var ns = GetServer(cbMode.Text);
                        if (ns == null) throw new XException("未识别服务[{0}]", mode);

                        _Server = ns.GetType().CreateInstance() as NetServer;
                    }
                    break;
            }

            if (_Client != null)
            {
                _Client.Log = cfg.ShowLog ? BizLog : Logger.Null;
                if (!local.Contains("所有本地")) _Client.Local.Host = local;
                _Client.Received += OnReceived;
                _Client.Remote.Port = port;
                _Client.Remote.Host = remote;

                _Client.LogSend = cfg.ShowSend;
                _Client.LogReceive = cfg.ShowReceive;

                if (!_Client.Open()) return;

                "已连接服务器".SpeechTip();
            }
            else if (_Server != null)
            {
                if (_Server == null) _Server = new NetServer();
                _Server.Log = cfg.ShowLog ? BizLog : Logger.Null;
                _Server.SocketLog = cfg.ShowSocketLog ? BizLog : Logger.Null;
                _Server.Port = port;
                if (!local.Contains("所有本地")) _Server.Local.Host = local;
                _Server.Received += OnReceived;

                _Server.LogSend = cfg.ShowSend;
                _Server.LogReceive = cfg.ShowReceive;

                //// 加大会话超时时间到1天
                //_Server.SessionTimeout = 24 * 3600;

                _Server.Start();

                "正在监听{0}".F(port).SpeechTip();
            }

            pnlSetting.Enabled = false;
            btnConnect.Text = "关闭";

            cfg.Save();

            _timer = new TimerX(ShowStat, null, 5000, 5000);
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
                BizLog.Info(msg);
            }
        }

        private void btnConnect_Click(Object sender, EventArgs e)
        {
            SaveConfig();

            var btn = sender as Button;
            if (btn.Text == "打开")
                Connect();
            else
                Disconnect();
        }

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
                //XTrace.WriteLine(line);

                BizLog.Info(line);
            }
        }

        Int32 _pColor = 0;
        Int32 BytesOfReceived = 0;
        Int32 BytesOfSent = 0;
        Int32 lastReceive = 0;
        Int32 lastSend = 0;
        private void timer1_Tick(Object sender, EventArgs e)
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

        private void btnSend_Click(Object sender, EventArgs e)
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

            var cfg = NetConfig.Current;

            // 处理换行
            str = str.Replace("\n", "\r\n");
            var buf = cfg.HexSend ? str.ToHex() : str.GetBytes();
            var pk = new Packet(buf);

            if (_Client != null)
            {
                if (ths <= 1)
                {
                    _Client.SendMulti(pk, count, sleep);
                }
                else
                {
                    // 多线程测试
                    //Parallel.For(0, ths, n =>
                    //{
                    //    var client = _Client.Remote.CreateRemote();
                    //    client.StatSend = _Client.StatSend;
                    //    client.StatReceive = _Client.StatReceive;
                    //    client.SendMulti(buf, count, sleep);
                    //});
                    var any = _Client.Local.Address.IsAny();
                    var list = new List<ISocketClient>();
                    for (var i = 0; i < ths; i++)
                    {
                        var client = _Client.Remote.CreateRemote();
                        if (!any) client.Local.EndPoint = new IPEndPoint(_Client.Local.Address, 2000 + i);
                        client.StatSend = _Client.StatSend;
                        client.StatReceive = _Client.StatReceive;
                        //client.SendMulti(buf, count, sleep);

                        list.Add(client);
                    }
                    Parallel.For(0, ths, n =>
                    {
                        var client = list[n];
                        client.SendMulti(pk, count, sleep);
                        //try
                        //{
                        //    client.Open();
                        //}
                        //catch { }
                    });
                }
            }
            else if (_Server != null)
            {
                TaskEx.Run(async () =>
                {
                    BizLog.Info("准备向[{0}]个客户端发送[{1}]次[{2}]的数据", _Server.SessionCount, count, buf.Length);
                    for (var i = 0; i < count && _Server != null; i++)
                    {
                        var sw = Stopwatch.StartNew();
                        var cs = await _Server.SendAllAsync(buf);
                        sw.Stop();
                        BizLog.Info("{3}/{4} 已向[{0}]个客户端发送[{1}]数据 {2:n0}ms", cs, buf.Length, sw.ElapsedMilliseconds, i + 1, count);
                        if (sleep > 0) await TaskEx.Delay(sleep);
                    }
                });
            }
        }
        #endregion

        #region 右键菜单
        private void mi清空_Click(Object sender, EventArgs e)
        {
            txtReceive.Clear();
            BytesOfReceived = 0;
        }

        private void mi清空2_Click(Object sender, EventArgs e)
        {
            txtSend.Clear();
            BytesOfSent = 0;
        }

        private void mi显示应用日志_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示网络日志_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示发送数据_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示接收数据_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }

        private void mi显示统计信息_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            NetConfig.Current.ShowStat = mi.Checked = !mi.Checked;
        }

        private void mi显示接收字符串_Click(Object sender, EventArgs e)
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

        private void cbMode_SelectedIndexChanged(Object sender, EventArgs e)
        {
            var mode = GetMode();
            if (mode == 0) return;

            switch (mode)
            {
                case WorkModes.TCP_Client:
                case WorkModes.UDP_Client:
                    break;
                default:
                case WorkModes.UDP_TCP:
                case WorkModes.UDP_Server:
                case WorkModes.TCP_Server:
                    break;
                case (WorkModes)0xFF:
                    // 端口
                    var ns = GetServer(cbMode.Text);
                    if (ns != null && ns.Port > 0) numPort.Value = ns.Port;

                    break;
            }
        }

        WorkModes GetMode()
        {
            var mode = cbMode.Text;
            if (String.IsNullOrEmpty(mode)) return 0;

            var list = EnumHelper.GetDescriptions<WorkModes>().Where(kv => kv.Value == mode).ToList();
            if (list.Count == 0) return (WorkModes)0xFF;

            return list[0].Key;
        }

        static String[] GetIPs()
        {
            var list = NetHelper.GetIPs().Select(e => e.ToString()).ToList();
            list.Insert(0, "所有本地IPv4/IPv6");
            list.Insert(1, IPAddress.Any.ToString());
            list.Insert(2, IPAddress.IPv6Any.ToString());

            return list.ToArray();
        }

        static Dictionary<String, Type> _ns;
        static Dictionary<String, Type> GetNetServers()
        {
            if (_ns != null) return _ns;

            lock (typeof(FrmMain))
            {
                if (_ns != null) return _ns;

                var dic = new Dictionary<String, Type>();
                foreach (var item in typeof(NetServer).GetAllSubclasses(true))
                {
                    try
                    {
                        var ns = item.CreateInstance() as NetServer;
                        if (ns != null) dic.Add(item.GetDisplayName() ?? ns.Name, item);
                    }
                    catch { }
                }

                return _ns = dic;
            }
        }

        static NetServer GetServer(String name)
        {
            Type t = null;
            if (!GetNetServers().TryGetValue(name, out t)) return null;

            return t.CreateInstance() as NetServer;
        }
    }
}