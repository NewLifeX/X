using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Remoting;
#if !NET4
using System.Net.Http;
using System.Net.Http.Headers;
#endif

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

        private static readonly Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
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

        #region 远程调用
#if !NET4
        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(this HttpClient client, String action, Object args = null)
        {
            if (client?.BaseAddress == null) throw new ArgumentNullException(nameof(client.BaseAddress));

            // 内容类型
            var jtype = "application/json";
            var acc = client.DefaultRequestHeaders.Accept;
            if (acc.Count == 0) acc.TryParseAdd(jtype);
            //if (!acc.Contains(jtype)) acc.Add(jtype);

            // 序列化参数，决定GET/POST
            Byte[] buf = null;
            var code = HttpStatusCode.OK;
            var ps = args?.ToDictionary();
            if (ps != null && ps.Any(e => e.Value != null && e.Value.GetType().GetTypeCode() == TypeCode.Object))
            {
                var content = new ByteArrayContent(ps.ToJson().GetBytes());
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var msg = await client.PostAsync(action, content);
                code = msg.StatusCode;
                //msg.EnsureSuccessStatusCode();
                buf = await msg.Content.ReadAsByteArrayAsync();
            }
            else
            {
                var url = GetUrl(action, ps);
                // 如果长度过大，还是走POST
                buf = await client.GetByteArrayAsync(url);
            }
            if (buf == null || buf.Length == 0) return default;

            // 异常处理
            if (code != HttpStatusCode.OK) throw new ApiException((Int32)code, buf.ToStr()?.Trim('\"'));

            // 原始数据
            var rtype = typeof(TResult);
            if (rtype == typeof(Byte[])) return (TResult)(Object)buf;
            if (rtype == typeof(Packet)) return (TResult)(Object)new Packet(buf);

            // 简单类型
            var str = buf.ToStr();
            if (rtype.GetTypeCode() != TypeCode.Object) return str.ChangeType<TResult>();

            // 反序列化
            var dic = new JsonParser(str).Decode() as IDictionary<String, Object>;
            //if (!dic.TryGetValue("data", out var data)) throw new InvalidDataException("未识别响应数据");
            if (dic == null) throw new InvalidDataException("未识别响应数据");

            //if (dic.TryGetValue("result", out var result))
            //{
            //    if (result is Boolean res && !res) throw new InvalidOperationException("远程错误，{0}".F(data));
            //}
            //else if (dic.TryGetValue("code", out var code))
            //{
            //    if (code is Int32 cd && cd != 0) throw new InvalidOperationException("远程{1}错误，{0}".F(data, cd));
            //}
            //else
            //{
            //    throw new InvalidDataException("未识别响应数据");
            //}

            return JsonHelper.Convert<TResult>(dic);
        }

        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="client">Http客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static TResult Invoke<TResult>(this HttpClient client, String action, Object args = null) => Task.Run(() => InvokeAsync<TResult>(client, action, args)).Result;

        private static String Encode(String data)
        {
            if (String.IsNullOrEmpty(data)) return String.Empty;

            return Uri.EscapeDataString(data).Replace("%20", "+");
        }

        private static String GetUrl(String action, IDictionary<String, Object> ps)
        {
            var url = action;
            if (ps != null && ps.Count > 0)
            {
                var sb = Pool.StringBuilder.Get();
                sb.Append(action);
                sb.Append("?");

                var first = true;
                foreach (var item in ps)
                {
                    if (!first) sb.Append("&");
                    first = false;

                    sb.AppendFormat("{0}={1}", Encode(item.Key), Encode("{0}".F(item.Value)));
                }

                url = sb.Put(true);
            }

            return url;
        }
#endif
        #endregion
    }
}