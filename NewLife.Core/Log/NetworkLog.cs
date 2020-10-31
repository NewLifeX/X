using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using NewLife.Common;
using NewLife.Net;
using NewLife.Security;

namespace NewLife.Log
{
    /// <summary>网络日志</summary>
    public class NetworkLog : Logger, IDisposable
    {
        /// <summary>服务端</summary>
        public String Server { get; set; }

        /// <summary>应用标识</summary>
        public String AppId { get; set; }

        /// <summary>客户端标识</summary>
        public String ClientId { get; set; }

        private ISocketRemote _client;
        private HttpClient _http;
        private readonly ConcurrentQueue<String> _Logs = new ConcurrentQueue<String>();
        private volatile Int32 _logCount;
        private Int32 _writing;

        /// <summary>实例化网络日志。默认广播到514端口</summary>
        public NetworkLog() => Server = new NetUri(NetType.Udp, IPAddress.Broadcast, 514) + "";

        /// <summary>指定日志服务器地址来实例化网络日志</summary>
        /// <param name="server"></param>
        public NetworkLog(String server) => Server = server;

        /// <summary>销毁</summary>
        public void Dispose()
        {
            // 销毁前把队列日志输出
            if (_logCount > 0)
            {
                if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
                    PushLog();
                else
                    Thread.Sleep(500);
            }

            _client.TryDispose();
            _http.TryDispose();
        }

        private void Send(String value)
        {
            if (_client != null)
                _client.Send(value);
            else if (_http != null)
                _http.PostAsync("", new StringContent(value)).Wait();
        }

        private Boolean _inited;
        private void Init()
        {
            if (_inited) return;

            var sys = SysConfig.Current;
            if (AppId.IsNullOrEmpty()) AppId = sys.Name;
            if (ClientId.IsNullOrEmpty())
            {
                try
                {
                    ClientId = NetHelper.MyIP() + "@" + Process.GetCurrentProcess().Id;
                }
                catch
                {
                    ClientId = Rand.NextString(8);
                }
            }

            var uri = new NetUri(Server);
            switch (uri.Type)
            {
                case NetType.Unknown:
                    break;
                case NetType.Tcp:
                case NetType.Udp:
                    _client = uri.CreateRemote();
                    break;
                case NetType.Http:
                case NetType.Https:
                case NetType.WebSocket:
                    var http = new HttpClient(new HttpClientHandler { UseProxy = false })
                    {
                        BaseAddress = new Uri(Server)
                    };
                    http.DefaultRequestHeaders.Add("X-AppId", AppId);
                    http.DefaultRequestHeaders.Add("X-ClientId", ClientId);
                    _http = http;
                    break;
                default:
                    break;
            }
            if (_client == null && _http == null) return;

            // 首先发送日志头
            Send(GetHead());

            _inited = true;
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            if (_logCount > 100) return;

            var e = WriteLogEventArgs.Current.Set(level);
            // 特殊处理异常对象
            if (args != null && args.Length == 1 && args[0] is Exception ex && (format.IsNullOrEmpty() || format == "{0}"))
                e = e.Set(null, ex);
            else
                e = e.Set(Format(format, args), null);

            // 推入队列
            _Logs.Enqueue(e.ToString());
            Interlocked.Increment(ref _logCount);

            // 异步写日志，实时。即使这里错误，定时器那边仍然会补上
            if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
            {
                ThreadPool.QueueUserWorkItem(s =>
                {
                    try
                    {
                        PushLog();
                    }
                    catch { }
                    finally
                    {
                        _writing = 0;
                    }
                });
            }
        }

        private void PushLog()
        {
            Init();

            // Tcp/Udp 和 Http 推送日志时需要不同的包大小
            var max = _http != null ? 8192 : 1460;

            var sb = new StringBuilder();
            while (_Logs.TryDequeue(out var msg))
            {
                Interlocked.Decrement(ref _logCount);

                if (sb.Length > 0 && sb.Length + msg.Length >= max)
                {
                    Send(sb.ToString());
                    sb.Clear();
                }

                if (sb.Length > 0) sb.AppendLine();
                sb.Append(msg);
            }

            if (sb.Length > 0) Send(sb.ToString());
        }
    }
}