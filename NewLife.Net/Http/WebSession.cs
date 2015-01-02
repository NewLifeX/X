using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Web;
using NewLife.Net.Sockets;
using NewLife.Reflection;

namespace NewLife.Net.Http
{
    /// <summary>Web会话</summary>
    public class WebSession : NetSession
    {
        //TODO 未实现Web会话

        #region 属性
        private WebServer _Server;
        /// <summary>服务器</summary>
        public new WebServer Server { get { return _Server; } set { _Server = value; } }

        static string _localServerIP;
        private static string LocalServerIP
        {
            get
            {
                if (_localServerIP == null) _localServerIP = "" + NetHelper.GetIPs().FirstOrDefault();
                return _localServerIP;
            }
        }

        internal bool IsLocal
        {
            get
            {
                string remoteIP = ClientEndPoint.Address.ToString();
                if (string.IsNullOrEmpty(remoteIP)) return false;
                if (!remoteIP.Equals("127.0.0.1") && !remoteIP.Equals("::1") && !remoteIP.Equals("::ffff:127.0.0.1")) return LocalServerIP.Equals(remoteIP);
                return true;
            }
        }

        internal string LocalIP
        {
            get
            {
                var localEndPoint = ClientEndPoint;
                if (localEndPoint != null && localEndPoint.Address != null) return localEndPoint.Address.ToString();
                return this.DefaultLocalHostIP;
            }
        }

        String _defaultLoalhostIP;
        private string DefaultLocalHostIP
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultLoalhostIP))
                {
#if !NET4
                    if (!Socket.SupportsIPv4 && Socket.OSSupportsIPv6)
#else
                    if (!Socket.OSSupportsIPv4 && Socket.OSSupportsIPv6)
#endif
                        _defaultLoalhostIP = "::1";
                    else
                        _defaultLoalhostIP = "127.0.0.1";
                }
                return _defaultLoalhostIP;
            }
        }
        #endregion

        #region 启动会话
        //void Start()
        //{

        //}
        #endregion

        #region 方法
        private string GetErrorResponseBody(int statusCode, string message)
        {
            string str = Messages.FormatErrorMessageBody(statusCode, Server.VirtualPath);
            if (!String.IsNullOrEmpty(message)) str = str + "\r\n<!--\r\n" + message + "\r\n-->";
            return str;
        }

        internal byte[] ReadRequestBytes(int maxBytes)
        {
            return Session.Receive();
            //try
            //{
            //    if (WaitForRequestBytes() == 0) return null;
            //    int available = _socket.Available;
            //    if (available > maxBytes) available = maxBytes;
            //    int count = 0;
            //    byte[] buffer = new byte[available];
            //    if (available > 0) count = _socket.Receive(buffer, 0, available, SocketFlags.None);
            //    if (count < available)
            //    {
            //        byte[] dst = new byte[count];
            //        if (count > 0) Buffer.BlockCopy(buffer, 0, dst, 0, count);
            //        buffer = dst;
            //    }
            //    return buffer;
            //}
            //catch
            //{
            //    return null;
            //}
        }

        //internal int WaitForRequestBytes()
        //{
        //    int available = 0;
        //    try
        //    {
        //        if (_socket.Available == 0)
        //        {
        //            _socket.Poll(0x186a0, SelectMode.SelectRead);
        //            if (_socket.Available == 0 && _socket.Connected) _socket.Poll(0x1c9c380, SelectMode.SelectRead);
        //        }
        //        available = _socket.Available;
        //    }
        //    catch
        //    {
        //    }
        //    return available;
        //}

        internal void Write100Continue() { WriteEntireResponseFromString(100, null, null, true); }

        internal void WriteBody(byte[] data, int offset, int length)
        {
            try
            {
                Send(data, offset, length);
            }
            catch (SocketException) { }
        }

        internal void WriteEntireResponseFromFile(string fileName, bool keepAlive)
        {
            if (!File.Exists(fileName))
                WriteErrorAndClose(404);
            else
            {
                var moreHeaders = MakeContentTypeHeader(fileName);
                if (moreHeaders == null)
                    WriteErrorAndClose(403);
                else
                {
                    bool flag = false;
                    try
                    {
                        using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            //int length = (int)stream.Length;
                            //byte[] buffer = new byte[length];
                            //int contentLength = stream.Read(buffer, 0, length);
                            string header = MakeResponseHeaders(200, moreHeaders, (Int32)fs.Length, keepAlive);
                            Send(header);
                            Send(fs);
                            flag = true;
                        }
                    }
                    catch (SocketException) { }
                    finally
                    {
                        if (!keepAlive || !flag) Dispose();
                    }
                }
            }
        }

        internal void WriteEntireResponseFromString(int statusCode, IDictionary<String, String> extraHeaders, string body, bool keepAlive)
        {
            try
            {
                int contentLength = (body != null) ? Encoding.UTF8.GetByteCount(body) : 0;
                string str = MakeResponseHeaders(statusCode, extraHeaders, contentLength, keepAlive);
                Send(str + body);
            }
            catch (SocketException) { }
            finally
            {
                if (!keepAlive) Dispose();
            }
        }

        internal void WriteErrorAndClose(int statusCode, string message = null)
        {
            var dic = new Dictionary<String, String>();
            dic["Content-type"] = "text/html;charset=utf-8";
            WriteEntireResponseFromString(statusCode, dic, GetErrorResponseBody(statusCode, message), false);
        }

        internal void WriteErrorWithExtraHeadersAndKeepAlive(int statusCode, IDictionary<String, String> extraHeaders)
        {
            WriteEntireResponseFromString(statusCode, extraHeaders, GetErrorResponseBody(statusCode, null), true);
        }

        internal void WriteHeaders(int statusCode, IDictionary<String, String> extraHeaders)
        {
            string s = MakeResponseHeaders(statusCode, extraHeaders, -1, false);
            try
            {
                Send(s);
            }
            catch (SocketException) { }
        }
        #endregion

        #region 辅助
        private static IDictionary<String, String> MakeContentTypeHeader(string fileName)
        {
            string str = null;
            switch (Path.GetExtension(fileName).ToLowerInvariant())
            {
                case ".bmp":
                    str = "image/bmp";
                    break;

                case ".css":
                    str = "text/css";
                    break;

                case ".gif":
                    str = "image/gif";
                    break;

                case ".ico":
                    str = "image/x-icon";
                    break;

                case ".htm":
                case ".html":
                    str = "text/html";
                    break;

                case ".jpe":
                case ".jpeg":
                case ".jpg":
                    str = "image/jpeg";
                    break;

                case ".js":
                    str = "application/x-javascript";
                    break;
            }
            if (str == null) return null;
            var dic = new Dictionary<String, String>();
            dic["Content-Type"] = str;
            return dic;
        }

        private static string MakeResponseHeaders(int statusCode, IDictionary<String, String> moreHeaders, int contentLength, bool keepAlive)
        {
            var header = new HttpHeader();
            header.StatusCode = statusCode;
            header.StatusDescription = HttpWorkerRequest.GetStatusDescription(statusCode);
            if (contentLength >= 0) header.ContentLength = contentLength;
            header.Headers["Server"] = typeof(WebServer).FullName + "/" + AssemblyX.Create(Assembly.GetExecutingAssembly()).CompileVersion;
            header.Headers["Date"] = DateTime.Now.ToUniversalTime().ToString("R", DateTimeFormatInfo.InvariantInfo);
            if (moreHeaders != null)
            {
                foreach (var item in moreHeaders)
                {
                    header.Headers[item.Key] = item.Value;
                }
            }
            if (!keepAlive) header.KeepAlive = false;
            return header.ToText();

            //var builder = new StringBuilder();
            //builder.Append(string.Concat(new object[] { "HTTP/1.1 ", statusCode, " ", HttpWorkerRequest.GetStatusDescription(statusCode), "\r\n" }));
            //builder.Append("Server: ASP.NET Development Server/" + Messages.VersionString + "\r\n");
            //builder.Append("Date: " + DateTime.Now.ToUniversalTime().ToString("R", DateTimeFormatInfo.InvariantInfo) + "\r\n");
            //if (contentLength >= 0) builder.Append("Content-Length: " + contentLength + "\r\n");
            //if (moreHeaders != null) builder.Append(moreHeaders);
            //if (!keepAlive) builder.Append("Connection: Close\r\n");
            //builder.Append("\r\n");
            //return builder.ToString();
        }
        #endregion
    }
}