using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Http
{
    /// <summary>迷你Http客户端。支持https和302跳转</summary>
    public class TinyHttpClient : DisposeBase
    {
        #region 属性
        /// <summary>客户端</summary>
        public TcpClient Client { get; set; }

        /// <summary>基础地址</summary>
        public Uri BaseAddress { get; set; }

        /// <summary>内容类型</summary>
        public String ContentType { get; set; }

        /// <summary>内容长度</summary>
        public Int32 ContentLength { get; private set; }

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }

        /// <summary>状态码</summary>
        public Int32 StatusCode { get; set; }

        /// <summary>状态描述</summary>
        public String StatusDescription { get; set; }

        /// <summary>超时时间。默认15s</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>头部集合</summary>
        public IDictionary<String, String> Headers { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);

        private Stream _stream;
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

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

        #region 异步核心方法
        /// <summary>获取网络数据流</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual async Task<Stream> GetStreamAsync(Uri uri)
        {
            var tc = Client;
            var ns = _stream;

            // 判断连接是否可用
            var active = false;
            try
            {
                active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
                if (active) return ns;

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
#if NET4
                //tc.Connect(remote.Address, remote.Port);
                await Task.Factory.FromAsync(tc.BeginConnect, tc.EndConnect, remote.Address, remote.Port, null);
#else
                await tc.ConnectAsync(remote.Address, remote.Port).ConfigureAwait(false);
#endif

                Client = tc;
                ns = tc.GetStream();

                if (BaseAddress == null) BaseAddress = new Uri(uri, "/");

                active = true;
            }

            // 支持SSL
            if (active)
            {
                if (uri.Scheme.EqualIgnoreCase("https"))
                {
                    var sslStream = new SslStream(ns, false, (sender, certificate, chain, sslPolicyErrors) => true);
#if NET4
                    sslStream.AuthenticateAsClient(uri.Host, new X509CertificateCollection(), SslProtocols.Tls, false);
#else
                    await sslStream.AuthenticateAsClientAsync(uri.Host, new X509CertificateCollection(), SslProtocols.Tls12, false).ConfigureAwait(false);
#endif
                    ns = sslStream;
                }

                _stream = ns;
            }

            return ns;
        }

        /// <summary>异步请求</summary>
        /// <param name="uri"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<Packet> SendDataAsync(Uri uri, Packet request)
        {
            var ns = await GetStreamAsync(uri).ConfigureAwait(false);

            // 发送
            if (request != null) await request.CopyToAsync(ns).ConfigureAwait(false);

            // 接收
            var buf = new Byte[64 * 1024];
#if NET4
            var count = ns.Read(buf, 0, buf.Length);
#else
            var source = new CancellationTokenSource(Timeout);

            var count = await ns.ReadAsync(buf, 0, buf.Length, source.Token).ConfigureAwait(false);
#endif

            return new Packet(buf, 0, count);
        }

        /// <summary>异步发出请求，并接收响应</summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual async Task<Packet> SendAsync(Uri uri, Byte[] data)
        {
            // 构造请求
            var req = BuildRequest(uri, data);

            StatusCode = -1;

            Packet rs = null;
            var retry = 5;
            while (retry-- > 0)
            {
                // 发出请求
                rs = await SendDataAsync(uri, req).ConfigureAwait(false);
                if (rs == null || rs.Count == 0) return null;

                // 解析响应
                rs = ParseResponse(rs);

                // 跳转
                if (StatusCode == 301 || StatusCode == 302)
                {
                    if (Headers.TryGetValue("Location", out var location) && !location.IsNullOrEmpty())
                    {
                        // 再次请求
                        var uri2 = new Uri(location);

                        if (uri.Host != uri2.Host || uri.Scheme != uri2.Scheme) Client.TryDispose();

                        uri = uri2;
                        continue;
                    }
                }

                break;
            }

            if (StatusCode != 200) throw new Exception($"{StatusCode} {StatusDescription}");

            // 如果没有收完数据包
            if (ContentLength > 0 && rs.Count < ContentLength)
            {
                var total = rs.Total;
                var last = rs;
                while (total < ContentLength)
                {
                    var pk = await SendDataAsync(null, null).ConfigureAwait(false);
                    last.Append(pk);

                    last = pk;
                    total += pk.Total;
                }
            }

            // chunk编码
            if (rs.Count > 0 && Headers["Transfer-Encoding"].EqualIgnoreCase("chunked"))
            {
                rs = await ReadChunkAsync(rs);
            }

            return rs;
        }

        /// <summary>读取分片，返回链式Packet</summary>
        /// <param name="body"></param>
        /// <returns></returns>
        protected virtual async Task<Packet> ReadChunkAsync(Packet body)
        {
            var rs = body;
            var last = body;
            var pk = body;
            while (true)
            {
                // 分析一个片段，如果该片段数据不足，则需要多次读取
                var chunk = ParseChunk(pk, out var len);
                if (len <= 0) break;

                // 第一个包需要替换，因为偏移量改变
                if (last == body)
                    rs = chunk;
                else
                    last.Append(chunk);

                last = chunk;

                // 如果该片段数据不足，则需要多次读取
                var total = chunk.Total;
                while (total < len)
                {
                    pk = await SendDataAsync(null, null).ConfigureAwait(false);

                    // 结尾的间断符号（如换行或00）。这里有可能一个数据包里面同时返回多个分片，暂时不支持
                    if (total + pk.Total > len) pk = pk.Slice(0, len - total);

                    last.Append(pk);
                    last = pk;
                    total += pk.Total;
                }

                // 读取新的数据片段，如果不存在则跳出
                pk = await SendDataAsync(null, null).ConfigureAwait(false);
                if (pk == null || pk.Total == 0) break;
            }

            return rs;
        }
        #endregion

        #region 同步核心方法
        /// <summary>获取网络数据流</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual Stream GetStream(Uri uri)
        {
            var tc = Client;
            var ns = _stream;

            // 判断连接是否可用
            var active = false;
            try
            {
                active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
                if (active) return ns;

                ns = tc?.GetStream();
                active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            }
            catch { }

            // 如果连接不可用，则重新建立连接
            if (!active)
            {
                var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

                var ms = (Int32)Timeout.TotalMilliseconds;
                tc.TryDispose();
                tc = new TcpClient { SendTimeout = ms, ReceiveTimeout = ms };
                tc.Connect(remote.Address, remote.Port);

                Client = tc;
                ns = tc.GetStream();

                if (BaseAddress == null) BaseAddress = new Uri(uri, "/");

                active = true;
            }

            // 支持SSL
            if (active)
            {
                if (uri.Scheme.EqualIgnoreCase("https"))
                {
                    var sslStream = new SslStream(ns, false, (sender, certificate, chain, sslPolicyErrors) => true);
#if NET4
                    sslStream.AuthenticateAsClient(uri.Host, new X509CertificateCollection(), SslProtocols.Tls, false);
#else
                    sslStream.AuthenticateAsClient(uri.Host, new X509CertificateCollection(), SslProtocols.Tls12, false);
#endif
                    ns = sslStream;
                }

                _stream = ns;
            }

            return ns;
        }

        /// <summary>异步请求</summary>
        /// <param name="uri"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual Packet SendData(Uri uri, Packet request)
        {
            var ns = GetStream(uri);

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

            Packet rs = null;
            var retry = 5;
            while (retry-- > 0)
            {
                // 发出请求
                rs = SendData(uri, req);
                if (rs == null || rs.Count == 0) return null;

                // 解析响应
                rs = ParseResponse(rs);

                // 跳转
                if (StatusCode == 301 || StatusCode == 302)
                {
                    if (Headers.TryGetValue("Location", out var location) && !location.IsNullOrEmpty())
                    {
                        // 再次请求
                        var uri2 = new Uri(location);

                        if (uri.Host != uri2.Host || uri.Scheme != uri2.Scheme) Client.TryDispose();

                        uri = uri2;
                        continue;
                    }
                }

                break;
            }

            if (StatusCode != 200) throw new Exception($"{StatusCode} {StatusDescription}");

            // 如果没有收完数据包
            if (ContentLength > 0 && rs.Count < ContentLength)
            {
                var total = rs.Total;
                var last = rs;
                while (total < ContentLength)
                {
                    var pk = SendData(null, null);
                    last.Append(pk);

                    last = pk;
                    total += pk.Total;
                }
            }

            // chunk编码
            if (rs.Count > 0 && Headers["Transfer-Encoding"].EqualIgnoreCase("chunked"))
            {
                rs = ReadChunk(rs);
            }

            return rs;
        }

        /// <summary>读取分片，返回链式Packet</summary>
        /// <param name="body"></param>
        /// <returns></returns>
        protected virtual Packet ReadChunk(Packet body)
        {
            var rs = body;
            var last = body;
            var pk = body;
            while (true)
            {
                // 分析一个片段，如果该片段数据不足，则需要多次读取
                var chunk = ParseChunk(pk, out var len);
                if (len <= 0) break;

                // 第一个包需要替换，因为偏移量改变
                if (last == body)
                    rs = chunk;
                else
                    last.Append(chunk);

                last = chunk;

                // 如果该片段数据不足，则需要多次读取
                var total = chunk.Total;
                while (total < len)
                {
                    pk = SendData(null, null);

                    // 结尾的间断符号（如换行或00）。这里有可能一个数据包里面同时返回多个分片，暂时不支持
                    if (total + pk.Total > len) pk = pk.Slice(0, len - total);

                    last.Append(pk);
                    last = pk;
                    total += pk.Total;
                }

                // 读取新的数据片段，如果不存在则跳出
                pk = SendData(null, null);
                if (pk == null || pk.Total == 0) break;
            }

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
            StatusDescription = ss.Skip(2).Join(" ");
            //if (code == 302) return null;
            if (code >= 400) throw new Exception($"{code} {StatusDescription}");

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
        private Packet ParseChunk(Packet rs, out Int32 octets)
        {
            // chunk编码
            // 1 ba \r\n xxxx \r\n 0 \r\n\r\n

            octets = 0;
            var p = rs.IndexOf(NewLine);
            if (p <= 0) return rs;

            // 第一段长度
            var str = rs.Slice(0, p).ToStr();
            //if (str.Length % 2 != 0) str = "0" + str;
            //var len = (Int32)str.ToHex().ToUInt32(0, false);
            //Int32.TryParse(str, NumberStyles.HexNumber, null, out var len);
            octets = Int32.Parse(str, NumberStyles.HexNumber);

            //if (ContentLength < 0) ContentLength = len;

            return rs.Slice(p + 2, octets);
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
                return (await client.SendAsync(uri, null).ConfigureAwait(false))?.ToStr();
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

        #region 远程调用
        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public TResult Invoke<TResult>(String action, Object args = null)
        {
            if (BaseAddress == null) throw new ArgumentNullException(nameof(BaseAddress));

            var uri = new Uri(BaseAddress, action);

            // 序列化参数，决定GET/POST
            Packet pk = null;
            if (args != null)
            {
                var ps = args.ToDictionary();
                if (ps.Any(e => e.Value != null && e.Value.GetType().GetTypeCode() == TypeCode.Object))
                    pk = ps.ToJson().GetBytes();
                else
                {
                    var sb = Pool.StringBuilder.Get();
                    sb.Append(uri);
                    sb.Append("?");

                    var first = true;
                    foreach (var item in ps)
                    {
                        if (!first) sb.Append("&");
                        first = false;

                        sb.AppendFormat("{0}={1}", item.Key, HttpUtility.UrlEncode("{0}".F(item.Value)));
                    }

                    uri = new Uri(sb.Put(true));
                }
            }

            Packet rs = null;
            var pool = GetPool(uri.Host);
            var client = pool.Get();
            try
            {
                rs = client.Send(uri, pk.ToArray());
            }
            finally
            {
                pool.Put(client);
            }

            if (rs == null || rs.Total == 0) return default;

            var str = rs.ToStr();
            if (Type.GetTypeCode(typeof(TResult)) != TypeCode.Object) return str.ChangeType<TResult>();

            // 反序列化
            var dic = new JsonParser(str).Decode() as IDictionary<String, Object>;
            if (!dic.TryGetValue("data", out var data)) throw new InvalidDataException("未识别响应数据");

            if (dic.TryGetValue("result", out var result))
            {
                if (result is Boolean res && !res) throw new InvalidOperationException("远程错误，{0}".F(data));
            }
            else if (dic.TryGetValue("code", out var code))
            {
                if (code is Int32 cd && cd != 0) throw new InvalidOperationException("远程{1}错误，{0}".F(data, cd));
            }
            else
            {
                throw new InvalidDataException("未识别响应数据");
            }

            return JsonHelper.Convert<TResult>(data);
        }
        #endregion
    }
}