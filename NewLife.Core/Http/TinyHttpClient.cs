using System;
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
    /// <summary>迷你Http客户端</summary>
    public class TinyHttpClient : DisposeBase
    {
        #region 属性
        /// <summary>客户端</summary>
        public TcpClient Client { get; set; }

        /// <summary>内容类型</summary>
        public String ContentType { get; set; }

        /// <summary>内容长度</summary>
        public Int32 ContentLength { get; private set; }

        /// <summary>状态码</summary>
        public Int32 StatusCode { get; set; }
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Client.TryDispose();
        }
        #endregion

        #region 核心方法
        /// <summary>异步请求</summary>
        /// <param name="remote"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual async Task<Packet> SendDataAsync(NetUri remote, Packet request, Packet response)
        {
            var tc = Client;
            if (tc == null)
            {
                tc = new TcpClient();
                await tc.ConnectAsync(remote.Address, remote.Port);

                Client = tc;
            }

            var ns = tc.GetStream();

            // 发送
            //await ns.WriteAsync(data, 0, data.Length);
            request?.WriteTo(ns);

            var source = new CancellationTokenSource(15000);

            // 接收
            var buf = response.Data;
            var count = await ns.ReadAsync(buf, 0, buf.Length, source.Token);

            return response.Sub(0, count);
        }

        /// <summary>异步发出请求，并接收响应</summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual async Task<Packet> SendAsync(String url, Byte[] data, Packet response)
        {
            var uri = new Uri(url);
            var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

            // 构造请求
            var req = BuildRequest(uri, data);

            StatusCode = -1;

            // 发出请求
            var rs = await SendDataAsync(remote, req, response);
            if (rs == null || rs.Count == 0) return null;

            // 解析响应
            rs = ParseResponse(rs);

            // 头部和主体分两个包回来
            if (rs != null && rs.Count == 0 && ContentLength > 0) return await SendDataAsync(null, null, response);

            return rs;
        }

        /// <summary>构造请求头</summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual Packet BuildRequest(Uri uri, Byte[] data)
        {
            // GET / HTTP/1.1

            var method = data != null && data.Length > 0 ? "POST" : "GET";

            var header = new StringBuilder();
            header.AppendLine($"{method} {uri.PathAndQuery} HTTP/1.1");
            header.AppendLine($"Host: {uri.Host}");

            if (!ContentType.IsNullOrEmpty()) header.AppendLine($"Content-Type: {ContentType}");

            // 主体数据长度
            if (data != null && data.Length > 0) header.AppendLine($"Content-Length: {data.Length}");

            header.AppendLine();

            var req = new Packet(header.ToString().GetBytes());

            // 请求主体数据
            if (data != null && data.Length > 0) req.Next = new Packet(data);

            return req;
        }

        /// <summary>解析响应</summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        protected virtual Packet ParseResponse(Packet rs)
        {
            var p = (Int32)rs.Data.IndexOf(rs.Offset, rs.Count, "\r\n\r\n".GetBytes());
            // 兼容某些非法响应
            if (p < 0) p = (Int32)rs.Data.IndexOf(rs.Offset, rs.Count, "\r\n\n".GetBytes());
            if (p < 0) return null;

            var str = rs.ReadBytes(0, p).ToStr();
            var lines = str.Split("\r\n");

            // HTTP/1.1 502 Bad Gateway

            var ss = lines[0].Split(" ");
            if (ss.Length < 3) return null;

            // 分析响应码
            var code = StatusCode = ss[1].ToInt();
            //if (code == 302) return null;
            if (code != 200) throw new Exception($"{code} {ss.Skip(2).Join(" ")}");

            var len = -1;
            foreach (var item in lines)
            {
                if (item.StartsWithIgnoreCase("Content-Length:"))
                {
                    ContentLength = len = item.Split(":").LastOrDefault().ToInt();
                    break;
                }
            }

            return rs.Sub(p + 4, len);
        }
        #endregion

        #region 缓冲池
        class MyPool : Pool<Byte[]>
        {
            protected override Byte[] Create() => new Byte[64 * 1024];
        }

        private static MyPool _Pool = new MyPool
        {
            Max = 1000,
            Min = 2,
            IdleTime = 10,
            AllIdleTime = 60,
        };
        #endregion

        #region 主要方法
        /// <summary>异步发出请求，并接收响应</summary>
        /// <param name="url">地址</param>
        /// <param name="data">POST数据</param>
        /// <returns></returns>
        public async Task<Byte[]> SendAsync(String url, Byte[] data)
        {
            using (var pi = _Pool.AcquireItem())
            {
                // 发出请求
                var rs = await SendAsync(url, data, pi.Value);
                if (rs == null || rs.Count == 0) return null;

                return rs.ToArray();
            }
        }

        /// <summary>异步获取</summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public async Task<String> GetAsync(String url)
        {
            var rs = await SendAsync(url, null);
            return rs?.ToStr();
        }
        #endregion
    }
}