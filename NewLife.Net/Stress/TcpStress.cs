using System;
using System.Net;
using System.Text;
using System.Threading;
using NewLife.Log;
using System.Diagnostics;

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

        TcpStressClient[] cs;
        IPEndPoint _address;
        Byte[] _buffer;
        Random _rnd;

        Timer timer;
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
                timerShowStatus = new Timer(ShowStatus, null, 1000, 1000);

                stress.Start();
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            Console.WriteLine("压力测试中！任意键结束……");
            Console.ReadKey(true);

            stress.Stop();
            timerShowStatus.Dispose();
        }

        static Timer timerShowStatus;
        static Stopwatch sw;

        static void ShowStatus(Object state)
        {
            Console.Title = String.Format("连接:{1:n0} 消息:{2:n0} 速度:{3:n0}kB/s 总消息:{4:n0} 流量:{5:n0}kB 时间:{0:mm:ss}",
                sw.Elapsed,
                stress.Connections,
                stress.MessagesPerSecond,
                stress.BytesPerSecond / 1024,
                stress.Messages,
                stress.Bytes / 1024
                );
        }
        #endregion

        #region 方法
        /// <summary>初始化工作</summary>
        public void Init()
        {
            var cfg = _Config;

            cs = new TcpStressClient[cfg.Connections];
            _address = new IPEndPoint(Dns.GetHostAddresses(cfg.Address)[0], cfg.Port);
            _rnd = new Random((Int32)DateTime.Now.Ticks);

            // 初始化数据
            if (cfg.WaitForSend >= 0)
            {
                if (!String.IsNullOrEmpty(cfg.Data)) _buffer = Encoding.UTF8.GetBytes(cfg.Data);
                if (_buffer == null || _buffer.Length < 1)
                {
                    if (cfg.MinDataLength < 1) cfg.MinDataLength = 1;
                    if (cfg.MaxDataLength <= 0) cfg.MaxDataLength = 1500;

                    // 按最大大小分配数据，实际发送的数据在最小长度到最大长度之间
                    _buffer = new Byte[cfg.MaxDataLength];
                    _rnd.NextBytes(_buffer);
                }
            }
        }

        /// <summary>开始</summary>
        public void Start()
        {
            var cfg = _Config;
            var interval = cfg.Interval;

            for (int i = 0; i < cs.Length; i++)
            {
                try
                {
                    var client = cs[i] = new TcpStressClient();
                    client.Config = cfg;
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
            timer = new Timer(OnTimer, null, 1000, 1000);
        }

        void OnTimer(Object state)
        {
            _MessagesPerSecond = 0;
            _BytesPerSecond = 0;
        }

        /// <summary>停止</summary>
        public void Stop()
        {
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
        }
        #endregion
    }
}