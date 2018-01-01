using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Windows;
using XCoder;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace XMessage
{
    [DisplayName("消息调试工具")]
    public partial class FrmMain : Form, IXForm
    {
        NetServer _Server;
        ISocketClient _Client;
        IPacket _Packet;

        static Task<Type[]> _packets;
        //static Task<Type[]> _factorys;

        /// <summary>业务日志输出</summary>
        ILog BizLog;

        #region 窗体
        static FrmMain()
        {
            //_packets = TaskEx.Run(() => typeof(IPacket).GetAllSubclasses(true).ToArray());
            _packets = TaskEx.Run(() => typeof(IPacketFactory).GetAllSubclasses(true).ToArray());
        }

        public FrmMain()
        {
            InitializeComponent();

            Icon = IcoHelper.GetIcon("消息");
        }

        private void FrmMain_Load(Object sender, EventArgs e)
        {
            var log = TextFileLog.Create(null, "Message_{0:yyyy_MM_dd}.log");
            BizLog = txtReceive.Combine(log);
            txtReceive.UseWinFormControl();

            txtReceive.SetDefaultStyle(12);
            txtSend.SetDefaultStyle(12);
            numMutilSend.SetDefaultStyle(12);

            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            var cfg = MessageConfig.Current;
            cbMode.SelectedItem = cbMode.Items[0] + "";
            if (!cfg.Address.IsNullOrEmpty())
            {
                //cbAddr.DropDownStyle = ComboBoxStyle.DropDownList;
                cbAddr.DataSource = cfg.Address.Split(";");
            }

            // 加载封包协议
            foreach (var item in _packets.Result)
            {
                cbPacket.Items.Add(item.GetDisplayName() ?? item.Name);
            }
            cbPacket.SelectedIndex = 0;

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
            var cfg = MessageConfig.Current;
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
        }

        void SaveConfig()
        {
            var cfg = MessageConfig.Current;
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

            cfg.Save();
        }
        #endregion

        #region 收发数据
        void Connect()
        {
            _Server = null;
            _Client = null;

            var uri = new NetUri(cbAddr.Text);
            // 网络封包
            var idx = cbPacket.SelectedIndex;
            var fact = idx < 0 ? null : _packets.Result[idx].CreateInstance() as IPacketFactory;
            _Packet = fact.Create();

            var cfg = MessageConfig.Current;
            var log = BizLog;

            switch (cbMode.Text)
            {
                case "服务端":
                    var svr = new NetServer();
                    svr.Log = cfg.ShowLog ? log : Logger.Null;
                    svr.SocketLog = cfg.ShowSocketLog ? log : Logger.Null;
                    svr.Port = uri.Port;
                    if (uri.IsTcp || uri.IsUdp) svr.ProtocolType = uri.Type;
                    svr.MessageReceived += OnReceived;

                    svr.LogSend = cfg.ShowSend;
                    svr.LogReceive = cfg.ShowReceive;

                    // 加大会话超时时间到1天
                    //svr.SessionTimeout = 24 * 3600;

                    svr.SessionPacket = fact;

                    svr.Start();

                    "正在监听{0}".F(svr.Port).SpeechTip();

                    if (uri.Port == 0) uri.Port = svr.Port;
                    _Server = svr;
                    break;
                case "客户端":
                    var client = uri.CreateRemote();
                    client.Log = cfg.ShowLog ? log : Logger.Null;
                    client.MessageReceived += OnReceived;

                    client.LogSend = cfg.ShowSend;
                    client.LogReceive = cfg.ShowReceive;

                    client.Packet = _Packet;

                    client.Open();

                    "已连接服务器".SpeechTip();

                    if (uri.Port == 0) uri.Port = client.Port;
                    _Client = client;
                    break;
                default:
                    return;
            }

            pnlSetting.Enabled = false;
            btnConnect.Text = "关闭";

            // 添加地址
            var addr = uri.ToString();
            var list = cfg.Address.Split(";").ToList();
            if (!list.Contains(addr))
            {
                list.Insert(0, addr);
                cfg.Address = list.Join(";");
            }

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
            if (!MessageConfig.Current.ShowStat) return;

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

        void OnReceived(Object sender, MessageEventArgs e)
        {
            var session = sender as ISocketSession;
            if (session == null)
            {
                var ns = sender as INetSession;
                if (ns == null) return;
                session = ns.Session;
            }

            if (MessageConfig.Current.ShowReceiveString)
            {
                var line = e.Message.Payload.ToStr();
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

                var set = MessageConfig.Current;
                if (set.ColorLog) txtReceive.ColourDefault(_pColor);
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

            var cfg = MessageConfig.Current;

            // 处理换行
            str = str.Replace("\n", "\r\n");
            var buf = cfg.HexSend ? str.ToHex() : str.GetBytes();

            // 构造消息
            var msg = _Packet.CreateMessage(buf);
            //buf = msg.ToArray();
            var pk = msg.ToPacket();

            if (_Client != null)
            {
                if (ths <= 1)
                {
                    _Client.SendMulti(pk, count, sleep);
                }
                else
                {
                    Parallel.For(0, ths, n =>
                    {
                        var client = _Client.Remote.CreateRemote();
                        client.StatSend = _Client.StatSend;
                        client.StatReceive = _Client.StatReceive;
                        client.SendMulti(pk, count, sleep);
                    });
                }
            }
            else if (_Server != null)
            {
                buf = pk.ToArray();
                TaskEx.Run(async () =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var cs = await _Server.SendAllAsync(buf);
                        BizLog.Info("已向[{0}]个客户端发送[{1}]数据", cs, buf.Length);
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
            MessageConfig.Current.ShowStat = mi.Checked = !mi.Checked;
        }

        private void mi显示接收字符串_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            MessageConfig.Current.ShowReceiveString = mi.Checked = !mi.Checked;
        }

        private void miHex发送_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            MessageConfig.Current.HexSend = mi.Checked = !mi.Checked;
        }

        private void miCheck_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }
        #endregion
    }
}