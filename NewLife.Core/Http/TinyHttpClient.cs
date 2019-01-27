using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Net;

namespace NewLife.Http
{
    /// <summary>迷你Http客户端。不支持https和302跳转</summary>
    public class TinyHttpClient : DisposeBase
    {
        #region 属性
        /// <summary>客户端</summary>
        public TcpClient Client { get; set; }

        /// <summary>内容类型</summary>
        public String ContentType { get; set; }

        /// <summary>内容长度</summary>
        public Int32 ContentLength { get; private set; }

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }

        /// <summary>状态码</summary>
        public Int32 StatusCode { get; set; }

        /// <summary>超时时间。默认15s</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>头部集合</summary>
        public IDictionary<String, String> Headers { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Client.TryDispose();

            if (_Cache != null)
            {
                foreach (var item in _Cache)
                {
                    item.Value.TryDispose();
                }
            }
        }
        #endregion

        #region 核心方法
        /// <summary>异步请求</summary>
        /// <param name="uri"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<Packet> SendDataAsync(Uri uri, Packet request)
        {
            var tc = Client;
            NetworkStream ns = null;

            // 判断连接是否可用
            var active = false;
            try
            {
                ns = tc?.GetStream();
                active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            }
            catch { }

            // 如果连接不可用，则重新建立连接
            if (!active)
            {
                var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

                tc.TryDispose();
                tc = new TcpClient { ReceiveTimeout = (Int32)Timeout.TotalMilliseconds };
                await tc.ConnectAsync(remote.Address, remote.Port);

                Client = tc;
                ns = tc.GetStream();
            }

            // 发送
            if (request != null) await request.CopyToAsync(ns);

            // 接收
            var buf = new Byte[64 * 1024];
            var source = new CancellationTokenSource(Timeout);
            var count = await ns.ReadAsync(buf, 0, buf.Length, source.Token);

            return new Packet(buf, 0, count);
        }

        /// <summary>异步发出请求，并接收响应</summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual async Task<Packet> SendAsync(Uri uri, Byte[] data)
        {
            var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

            // 构造请求
            var req = BuildRequest(uri, data);

            StatusCode = -1;

            // 发出请求
            var rs = await SendDataAsync(uri, req);
            if (rs == null || rs.Count == 0) return null;

            // 解析响应
            rs = ParseResponse(rs);

            // 头部和主体分两个包回来
            if (rs != null && rs.Count == 0 && ContentLength != 0)
            {
                rs = await SendDataAsync(null, null);
            }

            // chunk编码
            if (rs.Count > 0 && Headers["Transfer-Encoding"].EqualIgnoreCase("chunked")) rs = ParseChunk(rs);

            return rs;
        }
        #endregion

        #region 同步核心
        /// <summary>同步请求</summary>
        /// <param name="uri"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual Packet SendData(Uri uri, Packet request)
        {
            var tc = Client;
            NetworkStream ns = null;

            // 判断连接是否可用
            var active = false;
            try
            {
                ns = tc?.GetStream();
                active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            }
            catch { }

            // 如果连接不可用，则重新建立连接
            if (!active)
            {
                var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

                tc.TryDispose();
                tc = new TcpClient { ReceiveTimeout = (Int32)Timeout.TotalMilliseconds };
                tc.Connect(remote.Address, remote.Port);

                Client = tc;
                ns = tc.GetStream();
            }

            // 发送
            if (request != null) request.CopyTo(ns);

            // 接收
            var buf = new Byte[64 * 1024];
            var count = ns.Read(buf, 0, buf.Length);

            return new Packet(buf, 0, count);
        }

        /// <summary>异步发出请求，并接收响应</summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual Packet Send(Uri uri, Byte[] data)
        {
            // 构造请求
            var req = BuildRequest(uri, data);

            StatusCode = -1;

            // 发出请求
            var rs = SendData(uri, req);
            if (rs == null || rs.Count == 0) return null;

            // 解析响应
            rs = ParseResponse(rs);

            // 头部和主体分两个包回来
            if (rs != null && rs.Count == 0 && ContentLength != 0)
            {
                rs = SendData(null, null);
            }

            // chunk编码
            if (rs.Count > 0 && Headers["Transfer-Encoding"].EqualIgnoreCase("chunked")) rs = ParseChunk(rs);

            return rs;
        }
        #endregion

        #region 辅助
        /// <summary>构造请求头</summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual Packet BuildRequest(Uri uri, Byte[] data)
        {
            // GET / HTTP/1.1

            var method = data != null && data.Length > 0 ? "POST" : "GET";

            var header = Pool.StringBuilder.Get();
            header.AppendLine($"{method} {uri.PathAndQuery} HTTP/1.1");
            header.AppendLine($"Host: {uri.Host}");

            if (!ContentType.IsNullOrEmpty()) header.AppendLine($"Content-Type: {ContentType}");

            // 主体数据长度
            if (data != null && data.Length > 0) header.AppendLine($"Content-Length: {data.Length}");

            // 保持连接
            if (KeepAlive) header.AppendLine("Connection: keep-alive");

            header.AppendLine();

            var req = new Packet(header.Put(true).GetBytes());

            // 请求主体数据
            if (data != null && data.Length > 0) req.Next = new Packet(data);

            return req;
        }

        private static readonly Byte[] NewLine4 = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
        private static readonly Byte[] NewLine3 = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\n' };
        /// <summary>解析响应</summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        protected virtual Packet ParseResponse(Packet rs)
        {
            var p = rs.IndexOf(NewLine4);
            // 兼容某些非法响应
            if (p < 0) p = rs.IndexOf(NewLine3);
            if (p < 0) return null;

            var str = rs.ToStr(Encoding.ASCII, 0, p);
            var lines = str.Split("\r\n");

            // HTTP/1.1 502 Bad Gateway

            var ss = lines[0].Split(" ");
            if (ss.Length < 3) return null;

            // 分析响应码
            var code = StatusCode = ss[1].ToInt();
            //if (code == 302) return null;
            if (code != 200) throw new Exception($"{code} {ss.Skip(2).Join(" ")}");

            // 分析头部
            var hs = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in lines)
            {
                var p2 = item.IndexOf(':');
                if (p2 <= 0) continue;

                var key = item.Substring(0, p2);
                var value = item.Substring(p2 + 1).Trim();

                hs[key] = value;
            }
            Headers = hs;

            var len = -1;
            if (hs.TryGetValue("Content-Length", out str)) len = str.ToInt(-1);
            ContentLength = len;

            if (hs.TryGetValue("Connection", out str) && str.EqualIgnoreCase("Close")) Client.TryDispose();

            return rs.Slice(p + 4, len);
        }

        private static readonly Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n' };
        private Packet ParseChunk(Packet rs)
        {
            // chunk编码
            // 1 ba \r\n xxxx \r\n 0 \r\n\r\n

            var p = rs.IndexOf(NewLine);
            if (p <= 0) return rs;

            // 第一段长度
            var str = rs.Slice(0, p).ToStr();
            //if (str.Length % 2 != 0) str = "0" + str;
            //var len = (Int32)str.ToHex().ToUInt32(0, false);
            //Int32.TryParse(str, NumberStyles.HexNumber, null, out var len);
            var len = Int32.Parse(str, NumberStyles.HexNumber);

            if (ContentLength < 0) ContentLength = len;

            var pk = rs.Slice(p + 2, len);

            // 暂时不支持多段编码

            return pk;
        }
        #endregion

        #region 主要方法
        private ConcurrentDictionary<String, IPool<TinyHttpClient>> _Cache;

        /// <summary>根据主机获取对象池</summary>
        /// <param name="host"></param>
        /// <returns></returns>
        protected virtual IPool<TinyHttpClient> GetPool(String host)
        {
            if (_Cache == null)
            {
                lock (this)
                {
                    if (_Cache == null) _Cache = new ConcurrentDictionary<String, IPool<TinyHttpClient>>();
                }
            }
            return _Cache.GetOrAdd(host, k => new ObjectPool<TinyHttpClient>
            {
                Min = 0,
                Max = 1000,
                IdleTime = 10,
                AllIdleTime = 60
            });
        }

        /// <summary>异步获取</summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public async Task<String> GetStringAsync(String url)
        {
            var uri = new Uri(url);
            var pool = GetPool(uri.Host);
            var client = pool.Get();
            try
            {
                return (await client.SendAsync(uri, null))?.ToStr();
            }
            finally
            {
                pool.Put(client);
            }
        }

        /// <summary>同步获取</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public String GetString(String url)
        {
            var uri = new Uri(url);
            var pool = GetPool(uri.Host);
            var client = pool.Get();
            try
            {
                return client.Send(uri, null)?.ToStr();
            }
            finally
            {
                pool.Put(client);
            }
        }
        #endregion
    }
}