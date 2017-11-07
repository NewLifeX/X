using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private async Task<Packet> SendAsync(NetUri remote, Packet pk)
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
            pk.WriteTo(ns);

            var source = new CancellationTokenSource(15000);

            // 接收
            var buf = new Byte[64 * 1024];
            var count = await ns.ReadAsync(buf, 0, buf.Length, source.Token);

            return new Packet(buf, 0, count);
        }

        public async Task<Packet> SendAsync(String url, Byte[] data)
        {
            var method = data != null && data.Length > 0 ? "POST" : "GET";
            var uri = new Uri(url);
            var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

            #region 构造请求
            // GET / HTTP/1.1

            var header = new StringBuilder();
            header.AppendLine($"{method} {uri.PathAndQuery} HTTP/1.1");
            header.AppendLine($"Host: {uri.Host}");

            if (!ContentType.IsNullOrEmpty()) header.AppendLine($"Content-Type: {ContentType}");

            // 主体数据长度
            if (data != null && data.Length > 0) header.AppendLine($"Content-Length: {data.Length}");

            header.AppendLine();

            var pk = new Packet(header.ToString().GetBytes());

            // 请求主体数据
            if (data != null && data.Length > 0) pk.Next = new Packet(data);
            #endregion

            // 发出请求
            var rs = await SendAsync(remote, pk);
            if (rs == null || rs.Count == 0) return null;

            #region 解析响应
            var p = (Int32)rs.Data.IndexOf(rs.Offset, rs.Count, "\r\n\r\n".GetBytes());
            if (p < 0) p = (Int32)rs.Data.IndexOf(rs.Offset, rs.Count, "\r\n\n".GetBytes());
            if (p < 0) return null;

            var str = rs.ReadBytes(0, p).ToStr();
            var lines = str.Split("\r\n");

            // HTTP/1.1 502 Bad Gateway

            var ss = lines[0].Split(" ");
            if (ss.Length < 3) return null;

            // 分析响应码
            var code = ss[1].ToInt();
            if (code != 200) throw new Exception($"{code} {ss.Skip(2).Join(" ")}");

            var len = -1;
            foreach (var item in lines)
            {
                if (item.StartsWithIgnoreCase("Content-Length:"))
                {
                    len = item.Split(":").LastOrDefault().ToInt();
                    break;
                }
            }
            #endregion

            return rs.Sub(p + 4);
        }
        #endregion

        /// <summary>异步获取</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<String> GetAsync(String url)
        {
            var rs = await SendAsync(url, null);
            return rs?.ToStr();
        }
    }
}