using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Windows;
using XCoder;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace XApi
{
    [DisplayName("Api调试")]
    public partial class FrmMain : Form, IXForm
    {
        ApiServer _Server;
        ApiClient _Client;

        /// <summary>业务日志输出</summary>
        ILog BizLog;

        #region 窗体
        public FrmMain()
        {
            InitializeComponent();

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();

            //Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Icon = IcoHelper.GetIcon("Api");
        }

        private void FrmMain_Load(Object sender, EventArgs e)
        {
            var log = TextFileLog.Create(null, "Api_{0:yyyy_MM_dd}.log");
            BizLog = txtReceive.Combine(log);
            txtReceive.UseWinFormControl();

            txtReceive.SetDefaultStyle(12);
            txtSend.SetDefaultStyle(12);
            numMutilSend.SetDefaultStyle(12);

            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            var cfg = ApiConfig.Current;
            //cbMode.SelectedItem = cbMode.Items[0] + "";
            cbMode.SelectedItem = cfg.Mode;
            var flag = (cfg.Mode == "服务端");
            numPort.Enabled = flag;
            cbAddr.Enabled = !flag;

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
            var cfg = ApiConfig.Current;
            mi显示应用日志.Checked = cfg.ShowLog;
            mi显示编码日志.Checked = cfg.ShowEncoderLog;
            mi显示发送数据.Checked = cfg.ShowSend;
            mi显示接收数据.Checked = cfg.ShowReceive;
            mi显示统计信息.Checked = cfg.ShowStat;

            cbMode.SelectedItem = cfg.Mode;
            numPort.Value = cfg.Port;
            // 历史地址列表
            if (!cfg.Address.IsNullOrEmpty()) cbAddr.DataSource = cfg.Address.Split(";");

            txtSend.Text = cfg.SendContent;
            numMutilSend.Value = cfg.SendTimes;
            numSleep.Value = cfg.SendSleep;
            numThreads.Value = cfg.SendUsers;
            mi日志着色.Checked = cfg.ColorLog;
        }

        void SaveConfig()
        {
            var cfg = ApiConfig.Current;
            cfg.ShowLog = mi显示应用日志.Checked;
            cfg.ShowEncoderLog = mi显示编码日志.Checked;
            cfg.ShowSend = mi显示发送数据.Checked;
            cfg.ShowReceive = mi显示接收数据.Checked;
            cfg.ShowStat = mi显示统计信息.Checked;

            cfg.Mode = cbMode.SelectedItem + "";
            cfg.Port = (Int32)numPort.Value;
            cfg.AddAddress(cbAddr.Text);

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

            var port = (Int32)numPort.Value;
            var uri = new NetUri(cbAddr.Text);

            var cfg = ApiConfig.Current;
            var log = BizLog;

            switch (cbMode.Text)
            {
                case "服务端":
                    var svr = new ApiServer(port)
                    {
                        Log = cfg.ShowLog ? log : Logger.Null,
                        EncoderLog = cfg.ShowEncoderLog ? log : Logger.Null
                    };

                    if (cfg.ShowSend || cfg.ShowReceive)
                    {
                        var ns = svr.Server as NetServer;
                        ns.Log = log;
                        ns.LogSend = cfg.ShowSend;
                        ns.LogReceive = cfg.ShowReceive;
                    }

                    svr.Register<MyApiController>();
                    svr.Start();

                    "正在监听{0}".F(port).SpeechTip();

                    _Server = svr;
                    break;
                case "客户端":
                    var client = new ApiClient(uri + "")
                    {
                        Log = cfg.ShowLog ? log : Logger.Null,
                        EncoderLog = cfg.ShowEncoderLog ? log : Logger.Null
                    };

                    if (cfg.ShowSend || cfg.ShowReceive)
                    {
                        var ct = client.Client;
                        ct.Log = log;
                        ct.LogSend = cfg.ShowSend;
                        ct.LogReceive = cfg.ShowReceive;
                    }

                    _Client = client;
                    client.Open();
                    // 连接成功后拉取Api列表
                    GetApiAll();

                    "已连接服务器".SpeechTip();

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

            _timer = new TimerX(ShowStat, null, 5000, 5000) { Async = true };
        }

        async void GetApiAll()
        {
            var apis = await _Client.InvokeAsync<String[]>("Api/All");
            if (apis != null) this.Invoke(() =>
            {
                cbAction.Items.Clear();
                foreach (var item in apis)
                {
                    cbAction.Items.Add(item);
                }
                cbAction.SelectedIndex = 0;
                cbAction.Visible = true;
            });
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
                "停止服务".SpeechTip();
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
            if (!ApiConfig.Current.ShowStat) return;

            var msg = "";
            if (_Client != null)
                msg = _Client.GetStat();
            else if (_Server != null)
                msg = _Server.GetStat();

            if (_Invoke > 0)
            {
                var ms = (Double)_Cost / _Invoke / 1000;
                if (ms > 1)
                    msg += $" Invoke={_Invoke} {ms:n0}ms";
                else
                    msg += $" Invoke={_Invoke} {ms:n3}ms";

                if (_TotalCost > 0) msg += $" Speed={_Invoke * 1000d / _TotalCost:n0}tps";
            }

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

                var set = ApiConfig.Current;
                if (set.ColorLog) txtReceive.ColourDefault(_pColor);
                _pColor = txtReceive.TextLength;
            }
        }

        private async void btnSend_Click(Object sender, EventArgs e)
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

            var uri = new NetUri(cbAddr.Text);
            var cfg = ApiConfig.Current;

            // 处理换行
            str = str.Replace("\n", "\r\n");

            var act = cbAction.SelectedItem + "";
            var action = act.Substring(" ", "(");
            if (action.IsNullOrEmpty()) return;

            var rtype = act.Substring(null, " ").GetTypeEx();
            if (rtype == null) rtype = typeof(Object);
            var ps = act.Substring("(", ")").Split(",");

            // 构造消息，二进制优先
            Object args = null;
            if (ps.Length == 1 && ps[0].StartsWith("Packet "))
            {
                args = new Packet(str.GetBytes());
            }
            else
            {
                var dic = new JsonParser(str).Decode() as IDictionary<String, Object>;
                if (dic == null || dic.Count == 0) dic = null;
                args = dic;
            }

            if (_Client == null) return;

            _Invoke = 0;
            _Cost = 0;
            _TotalCost = 0;

            var ct = _Client.Client;
            var list = new List<ApiClient> { _Client };
            for (var i = 0; i < ths - 1; i++)
            {
                var client = new ApiClient(uri + "");
                var ct2 = client.Client;
                ct2.Log = ct.Log;
                ct2.LogSend = ct.LogSend;
                ct2.LogReceive = ct.LogReceive;
                ct2.StatSend = ct.StatSend;
                ct2.StatReceive = ct.StatReceive;

                client.StatSend = _Client.StatSend;
                client.StatReceive = _Client.StatReceive;

                list.Add(client);
            }
            //Parallel.ForEach(list, k => OnSend(k, act, args, count));
            var sw = Stopwatch.StartNew();
            var ts = list.Select(k => OnSend(k, rtype, action, args, count, sleep)).ToList();

            await TaskEx.WhenAll(ts);
            sw.Stop();
            _TotalCost = sw.Elapsed.TotalMilliseconds;
        }

        Int64 _Invoke;
        Int64 _Cost;
        Double _TotalCost;
        private async Task OnSend(ApiClient client, Type rtype, String act, Object args, Int32 count, Int32 sleep)
        {
            client.Open();

            // 间隔1开启同步发送不等待
            if (sleep == 1)
            {
                var sw = Stopwatch.StartNew();
                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        Interlocked.Increment(ref _Invoke);

                        client.Invoke(act, args);
                    }
                    catch (ApiException ex)
                    {
                        BizLog.Info(ex.Message);
                    }
                }
                sw.Stop();
                Interlocked.Add(ref _Cost, (Int64)(sw.Elapsed.TotalMilliseconds * 1000));
            }
            // 间隔2~10多任务异步发送
            else if (sleep <= 10)
            {
                var ts = new List<Task>();
                for (var i = 0; i < count; i++)
                {
                    ts.Add(TaskEx.Run(async () =>
                    {
                        try
                        {
                            //var sw = Stopwatch.StartNew();
                            await client.InvokeAsync(rtype, act, args);
                            //sw.Stop();

                            Interlocked.Increment(ref _Invoke);
                            //Interlocked.Add(ref _Cost, (Int64)(sw.Elapsed.TotalMilliseconds * 1000));
                        }
                        catch (ApiException ex)
                        {
                            BizLog.Info(ex.Message);
                        }
                    }));
                }

                await TaskEx.WhenAll(ts);
            }
            // 间隔>10单任务异步发送
            else
            {
                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        await client.InvokeAsync(rtype, act, args);
                        sw.Stop();

                        Interlocked.Increment(ref _Invoke);
                        Interlocked.Add(ref _Cost, (Int64)(sw.Elapsed.TotalMilliseconds * 1000));
                    }
                    catch (ApiException ex)
                    {
                        BizLog.Info(ex.Message);
                    }

                    await TaskEx.Delay(sleep);
                }
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

        private void miCheck_Click(Object sender, EventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            mi.Checked = !mi.Checked;
        }
        #endregion

        private void cbAction_SelectedIndexChanged(Object sender, EventArgs e)
        {
            if (!(sender is ComboBox cb)) return;

            var txt = cb.SelectedItem + "";
            if (txt.IsNullOrEmpty()) return;

            var set = ApiConfig.Current;

            // 截取参数部分
            var pis = txt.Substring("(", ")").Split(",");

            // 生成参数
            var ps = new Dictionary<String, Object>();
            foreach (var item in pis)
            {
                var ss = item.Split(" ");
                Object val = null;
                switch (ss[0])
                {
                    case "String":
                        val = "";
                        break;
                    case "Int32":
                        val = 0;
                        break;
                    default:
                        break;
                }
                ps[ss[1]] = val;
            }

            txtSend.Text = ps.ToJson();
        }

        private void cbMode_SelectedIndexChanged(Object sender, EventArgs e)
        {
            var mode = cbMode.SelectedItem + "";
            var flag = mode == "服务端";
            numPort.Enabled = flag;
            cbAddr.Enabled = !flag;
        }
    }
}