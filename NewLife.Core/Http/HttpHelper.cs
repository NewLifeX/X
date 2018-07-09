using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Security;

namespace NewLife.Http
{
    /// <summary>Http帮助类</summary>
    public static class HttpHelper
    {
        #region Http封包解包
        /// <summary>创建请求包</summary>
        /// <param name="method"></param>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet MakeRequest(String method, Uri uri, IDictionary<String, Object> headers, Packet pk)
        {
            if (method.IsNullOrEmpty()) method = pk?.Count > 0 ? "POST" : "GET";

            // 分解主机和资源
            var host = "";
            if (uri == null) uri = new Uri("/");

            if (uri.Scheme.EqualIgnoreCase("http", "ws"))
            {
                if (uri.Port == 80)
                    host = uri.Host;
                else
                    host = "{0}:{1}".F(uri.Host, uri.Port);
            }
            else if (uri.Scheme.EqualIgnoreCase("https"))
            {
                if (uri.Port == 443)
                    host = uri.Host;
                else
                    host = "{0}:{1}".F(uri.Host, uri.Port);
            }

            // 构建头部
            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("{0} {1} HTTP/1.1\r\n", method, uri.PathAndQuery);
            sb.AppendFormat("Host:{0}\r\n", host);

            //if (Compressed) sb.AppendLine("Accept-Encoding:gzip, deflate");
            //if (KeepAlive) sb.AppendLine("Connection:keep-alive");
            //if (!UserAgent.IsNullOrEmpty()) sb.AppendFormat("User-Agent:{0}\r\n", UserAgent);

            // 内容长度
            if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

            foreach (var item in headers)
            {
                sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }

            sb.AppendLine();

            //return sb.ToString();
            var rs = new Packet(sb.Put(true).GetBytes())
            {
                Next = pk
            };
            return rs;
        }

        /// <summary>创建响应包</summary>
        /// <param name="code"></param>
        /// <param name="headers"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet MakeResponse(HttpStatusCode code, IDictionary<String, Object> headers, Packet pk)
        {
            // 构建头部
            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("HTTP/1.1 {0} {1}\r\n", (Int32)code, code);

            // 内容长度
            if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

            foreach (var item in headers)
            {
                sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }

            sb.AppendLine();

            //return sb.ToString();
            var rs = new Packet(sb.Put(true).GetBytes())
            {
                Next = pk
            };
            return rs;
        }

        private static Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
        /// <summary>分析头部</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static IDictionary<String, Object> ParseHeader(Packet pk)
        {
            // 客户端收到响应，服务端收到请求
            var headers = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            var p = pk.IndexOf(NewLine);
            if (p < 0) return headers;

#if DEBUG
            //WriteLog(pk.ToStr());
#endif

            // 截取
            var lines = pk.ReadBytes(0, p).ToStr().Split("\r\n");
            // 重构
            p += 4;
            pk.Set(pk.Data, pk.Offset + p, pk.Count - p);

            // 分析头部
            headers.Clear();
            var line = lines[0];
            for (var i = 1; i < lines.Length; i++)
            {
                line = lines[i];
                p = line.IndexOf(':');
                if (p > 0) headers[line.Substring(0, p)] = line.Substring(p + 1).Trim();
            }

            line = lines[0];
            var ss = line.Split(" ");
            // 分析请求方法 GET / HTTP/1.1
            if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
            {
                headers["Method"] = ss[0];

                // 构造资源路径
                var host = headers["Host"] + "";
                var uri = "{0}://{1}".F("http", host);
                //var uri = "{0}://{1}".F(IsSSL ? "https" : "http", host);
                //if (host.IsNullOrEmpty() || !host.Contains(":"))
                //{
                //    var port = Local.Port;
                //    if (IsSSL && port != 443 || !IsSSL && port != 80) uri += ":" + port;
                //}
                uri += ss[1];
                headers["Url"] = new Uri(uri);
            }
            else
            {
                // 分析响应码
                var code = ss[1].ToInt();
                if (code > 0) headers["StatusCode"] = (HttpStatusCode)code;
            }

            return headers;
        }
        #endregion

        #region WebSocket
        /// <summary>建立握手包</summary>
        /// <param name="request"></param>
        public static void MakeHandshake(HttpRequest request)
        {
            request["Upgrade"] = "websocket";
            request["Connection"] = "Upgrade";
            request["Sec-WebSocket-Key"] = Rand.NextBytes(16).ToBase64();
            request["Sec-WebSocket-Version"] = "13";
        }

        /// <summary>握手</summary>
        /// <param name="key"></param>
        /// <param name="response"></param>
        public static void Handshake(String key, HttpResponse response)
        {
            if (key.IsNullOrEmpty()) return;

            var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
            key = buf.ToBase64();

            //var sb = new StringBuilder();
            //sb.AppendLine("HTTP/1.1 101 Switching Protocols");
            //sb.AppendLine("Upgrade: websocket");
            //sb.AppendLine("Connection: Upgrade");
            //sb.AppendLine("Sec-WebSocket-Accept: " + key);
            //sb.AppendLine();

            //return sb.ToString().GetBytes();

            response.StatusCode = HttpStatusCode.SwitchingProtocols;
            response.Headers["Upgrade"] = "websocket";
            response.Headers["Connection"] = "Upgrade";
            response.Headers["Sec-WebSocket-Accept"] = key;
        }

        /// <summary>分析WS数据包</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet ParseWS(Packet pk)
        {
            if (pk.Count < 2) return null;

            var ms = pk.GetStream();

            // 仅处理一个包
            var fin = (ms.ReadByte() & 0x80) == 0x80;
            if (!fin) return null;

            var len = ms.ReadByte();

            var mask = (len & 0x80) == 0x80;

            /*
             * 数据长度
             * len < 126    单字节表示长度
             * len = 126    后续2字节表示长度，大端
             * len = 127    后续8字节表示长度
             */
            len = len & 0x7F;
            if (len == 126)
                len = ms.ReadBytes(2).ToUInt16(0, false);
            else if (len == 127)
                // 没有人会传输超大数据
                len = (Int32)BitConverter.ToUInt64(ms.ReadBytes(8), 0);

            // 如果mask，剩下的就是数据，避免拷贝，提升性能
            if (!mask) return new Packet(pk.Data, pk.Offset + (Int32)ms.Position, len);

            var masks = new Byte[4];
            if (mask) masks = ms.ReadBytes(4);

            // 读取数据
            var data = ms.ReadBytes(len);

            if (mask)
            {
                for (var i = 0; i < len; i++)
                {
                    data[i] = (Byte)(data[i] ^ masks[i % 4]);
                }
            }

            return data;
        }

        /// <summary>创建WS请求包</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet MakeWS(Packet pk)
        {
            if (pk == null) return null;

            var size = pk.Count;

            var ms = new MemoryStream();
            ms.WriteByte(0x81);

            /*
             * 数据长度
             * len < 126    单字节表示长度
             * len = 126    后续2字节表示长度，大端
             * len = 127    后续8字节表示长度
             */
            if (size < 126)
                ms.WriteByte((Byte)size);
            else if (size < 0xFFFF)
            {
                ms.WriteByte(126);
                ms.Write(((Int16)size).GetBytes(false));
            }
            else
                throw new NotSupportedException();

            //pk.WriteTo(ms);

            //return new Packet(ms.ToArray());

            return new Packet(ms.ToArray()) { Next = pk };
        }
        #endregion
    }
}