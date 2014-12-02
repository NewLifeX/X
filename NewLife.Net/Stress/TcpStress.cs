using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;

namespace NewLife.Net.Stress
{
    /// <summary>Tcp压力测试</summary>
    public class TcpStress : DisposeBase
    {
        #region 属性
        private TcpStressConfig _Config;
        /// <summary>配置</summary>
        public TcpStressConfig Config { get { return _Config; } set { _Config = value; } }

        private Int32 _Connections;
        /// <summary>连接数</summary>
        public Int32 Connections { get { return _Connections; } }

        private Int32 _Messages;
        /// <summary>消息数</summary>
        public Int32 Messages { get { return _Messages; } set { _Messages = value; } }

        private Int32 _Bytes;
        /// <summary>字节数</summary>
        public Int32 Bytes { get { return _Bytes; } set { _Bytes = value; } }

        private Int32 _MessagesPerSecond;
        /// <summary>每秒消息数</summary>
        public Int32 MessagesPerSecond { get { return _MessagesPerSecond; } set { _MessagesPerSecond = value; } }

        private Int32 _BytesPerSecond;
        /// <summary>每秒字节数</summary>
        public Int32 BytesPerSecond { get { return _BytesPerSecond; } set { _BytesPerSecond = value; } }

        private Int32 _MaxMessages;
        /// <summary>最大消息速度</summary>
        public Int32 MaxMessages { get { return _MaxMessages; } set { _MaxMessages = value; } }

        private Int32 _MaxBytes;
        /// <summary>最大字节速度</summary>
        public Int32 MaxBytes { get { return _MaxBytes; } set { _MaxBytes = value; } }

        TcpStressClient[] cs;
        IPEndPoint _address;
        Byte[] _buffer;

        TimerX timer;
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Stop();
        }
        #endregion

        #region 测试主方法
        static TcpStress stress;
        /// <summary>入口方法</summary>
        public static void Main()
        {
            try
            {
                stress = new TcpStress();
                stress.Config = TcpStressConfig.Current;
                stress.Config.Show();
                stress.Init();

                Console.WriteLine("准备就绪！任意键开始……");
                Console.ReadKey(true);

                sw = new Stopwatch();
                sw.Start();
                timerShowStatus = new TimerX(ShowStatus, null, 1000, 1000);

                stress.Start();
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            Console.WriteLine("压力测试中！任意键结束……");
            Console.ReadKey(true);

            stress.Stop();
            timerShowStatus.Dispose();
        }

        static TimerX timerShowStatus;
        static Stopwatch sw;

        static void ShowStatus(Object state)
        {
            var ts = sw.Elapsed;
            var t = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            Console.Title = String.Format("连接:{1:n0} 消息:{2:n0}/s 速度:{3:n0}kB/s 最大消息：{4:n0} 最大速度：{5:n0}kB/s 总消息:{6:n0} 流量:{7:n0}kB 时间:{0}",
                t,
                stress.Connections,
                stress.MessagesPerSecond,
                stress.BytesPerSecond / 1024,
                stress.MaxMessages,
                stress.MaxBytes / 1024,
                stress.Messages,
                stress.Bytes / 1024
                );

            try
            {
                Int32 wt = 0;
                Int32 cpt = 0;
                ThreadPool.GetAvailableThreads(out wt, out cpt);
                Int32 threads = Process.GetCurrentProcess().Threads.Count;

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.CursorLeft = 0;
                Console.Write("连接:{0} 消息速度:{1}/s Thread:{2}/{3}/{4}", stress.Connections, stress.MessagesPerSecond, threads, wt, cpt);
                Console.ForegroundColor = color;
            }
            catch { }
        }
        #endregion

        #region 方法
        /// <summary>初始化工作</summary>
        public void Init()
        {
            var cfg = _Config;

            cs = new TcpStressClient[cfg.Connections];
            //_address = new IPEndPoint(Dns.GetHostAddresses(cfg.Address)[0], cfg.Port);
            _address = NetHelper.ParseEndPoint(cfg.Address, cfg.Port);

            // 初始化数据
            if (!String.IsNullOrEmpty(cfg.Data))
            {
                if (cfg.Data.StartsWithIgnoreCase("0x"))
                    _buffer = cfg.Data.Substring(2).Trim().ToHex();
                else
                    _buffer = Encoding.UTF8.GetBytes(cfg.Data);

                if (cfg.UseLength)
                {
                    var bts = BitConverter.GetBytes(_buffer.Length);
                    var buf = new Byte[bts.Length + _buffer.Length];
                    Buffer.BlockCopy(bts, 0, buf, 0, bts.Length);
                    Buffer.BlockCopy(_buffer, 0, buf, bts.Length, _buffer.Length);
                    _buffer = buf;
                }
            }
        }

        /// <summary>开始</summary>
        public void Start()
        {
            var cfg = _Config;
            var interval = cfg.Interval;

            XTrace.WriteLine("开始建立连接……");
            for (int i = 0; i < cs.Length; i++)
            {
                try
                {
                    var client = cs[i] = new TcpStressClient();
                    client.Interval = cfg.SendInterval;
                    client.Times = cfg.Times;
                    client.EndPoint = _address;
                    client.Buffer = _buffer;

                    client.Connected += (s, e) => Interlocked.Increment(ref _Connections);
                    client.Disconnected += (s, e) => Interlocked.Decrement(ref _Connections);
                    client.Sent += (s, e) => SendMessage(e.Arg);

                    client.ConnectAsync();
                }
                catch (Exception ex) { XTrace.WriteException(ex); }

                if (interval > 0) Thread.Sleep(interval);
            }

            // 定时器用于计算每秒统计数
            timer = new TimerX(OnTimer, null, 1000, 1000);

            // 开始发送
            if (_buffer != null && _buffer.Length > 0)
            {
                XTrace.WriteLine("开始发送数据……");
                for (int i = 0; i < cs.Length; i++)
                {
                    cs[i].StartSend();
                }
            }
        }

        void OnTimer(Object state)
        {
            _MessagesPerSecond = 0;
            _BytesPerSecond = 0;
        }

        /// <summary>停止</summary>
        public void Stop()
        {
            if (cs != null)
            {
                XTrace.WriteLine("正在关闭连接……");
                for (int i = 0; i < cs.Length; i++)
                {
                    if (cs[i] != null)
                    {
                        try
                        {
                            cs[i].Dispose();
                        }
                        catch { }
                    }
                }
                cs = null;
            }

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        void SendMessage(Int32 count)
        {
            Interlocked.Increment(ref _Messages);
            Interlocked.Increment(ref _MessagesPerSecond);
            Interlocked.Add(ref _Bytes, count);
            Interlocked.Add(ref _BytesPerSecond, count);

            if (_MessagesPerSecond > MaxMessages) MaxMessages = _MessagesPerSecond;
            if (_BytesPerSecond > MaxBytes) MaxBytes = _BytesPerSecond;
        }
        #endregion
    }
}