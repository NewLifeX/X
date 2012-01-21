using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using System.Web;
using System.Collections;
using System.Security;
using System.Web.Hosting;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace NewLife.Net.Http
{
    /// <summary>定义由 ASP.NET 托管代码用于处理请求的基本辅助方法和枚举</summary>
    sealed class WebRequest : SimpleWorkerRequest
    {
        #region 属性
        private WebSession _Session;
        /// <summary>会话</summary>
        public WebSession Session { get { return _Session; } set { _Session = value; } }

        private Host _Host;
        /// <summary>主机</summary>
        public Host Host { get { return _Host; } set { _Host = value; } }

        private HttpHeader _ResponseHeader;
        /// <summary>响应头</summary>
        public HttpHeader ResponseHeader { get { return _ResponseHeader ?? (_ResponseHeader = new HttpHeader()); } }

        private string _allRawHeaders;
        //private IStackWalk _connectionPermission;
        private int _contentLength;
        private int _endHeadersOffset;
        private byte[] _headerBytes;
        private ArrayList _headerByteStrings;
        private bool _isClientScriptPath;
        private string[] _knownRequestHeaders;
        private int _preloadedContentLength;
        private ArrayList _responseBodyBytes;
        //private StringBuilder _responseHeadersBuilder;
        //private int ResponseHeader.StatusCode;
        private bool _specialCaseStaticFileHeaders;
        private int _startHeadersOffset;
        private static char[] badPathChars = new char[] { '%', '>', '<', ':', '\\' };
        private static string[] defaultFileNames = new string[] { "default.aspx", "default.htm", "default.html" };
        private static char[] IntToHex = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        private const int MaxChunkLength = 0x10000;
        private const int maxHeaderBytes = 0x8000;
        private static string[] restrictedDirs = new string[] { "/bin", "/app_browsers", "/app_code", "/app_data", "/app_localresources", "/app_globalresources", "/app_webreferences" };
        #endregion

        #region 构造
        public WebRequest(Host host, WebSession session)
            : base(string.Empty, string.Empty, null)
        {
            //_connectionPermission = new PermissionSet(PermissionState.Unrestricted);
            Host = host;
            Session = session;
        }
        #endregion

        #region 信息属性
        /// <summary>返回当前正在执行的服务器应用程序的虚拟路径。</summary>
        /// <returns>当前应用程序的虚拟路径。</returns>
        public override string GetAppPath() { return Host.VirtualPath; }

        /// <summary>返回当前正在执行的服务器应用程序的物理路径。</summary>
        /// <returns>当前应用程序的物理路径。</returns>
        public override string GetAppPathTranslated() { return Host.PhysicalPath; }

        private string _filePath;
        /// <summary>在派生类中被重写时，返回所请求的 URI 的虚拟路径。</summary>
        /// <returns>请求的 URI 的路径。</returns>
        public override string GetFilePath() { return _filePath; }

        private string _pathTranslated;
        /// <summary>返回请求的 URI 的物理文件路径（并将其从虚拟路径转换成物理路径：例如，从“/proj1/page.aspx”转换成“c:\dir\page.aspx”）</summary>
        /// <returns>请求的 URI 的已转换的物理文件路径。</returns>
        public override string GetFilePathTranslated() { return _pathTranslated; }

        /// <summary>返回请求标头的指定成员。</summary>
        /// <returns>请求标头中返回的 HTTP 谓词。</returns>
        public override string GetHttpVerbName() { return ResponseHeader.Method; }

        /// <summary>提供对请求的 HTTP 版本（如“HTTP/1.1”）的访问。</summary>
        /// <returns>请求标头中返回的 HTTP 版本。</returns>
        public override string GetHttpVersion() { return ResponseHeader.Version; }

        /// <summary>返回与指定的索引相对应的标准 HTTP 请求标头。</summary>
        /// <returns>HTTP 请求标头。</returns>
        /// <param name="index">标头的索引。例如，<see cref="F:System.Web.HttpWorkerRequest.HeaderAllow" /> 字段。</param>
        public override string GetKnownRequestHeader(int index) { return _knownRequestHeaders[index]; }

        /// <summary>提供对请求标头的指定成员的访问。</summary>
        /// <returns>请求标头中返回的服务器 IP 地址。</returns>
        public override string GetLocalAddress()
        {
            //_connectionPermission.Assert();
            return Session.LocalIP;
        }

        /// <summary>提供对请求标头的指定成员的访问。</summary>
        /// <returns>请求标头中返回的服务器端口号。</returns>
        public override int GetLocalPort() { return Host.Port; }

        private string _pathInfo;
        /// <summary>返回具有 URL 扩展的资源的其他路径信息。即对于路径 /virdir/page.html/tail，GetPathInfo 值为 /tail。</summary>
        /// <returns>资源的附加路径信息。</returns>
        public override string GetPathInfo() { return _pathInfo; }

        private byte[] _preloadedContent;
        /// <summary>返回 HTTP 请求正文已被读取的部分。</summary>
        /// <returns>HTTP 请求正文已被读取的部分。</returns>
        public override byte[] GetPreloadedEntityBody() { return _preloadedContent; }

        private string _queryString;
        /// <summary>返回请求 URL 中指定的查询字符串。</summary>
        /// <returns>请求查询字符串。</returns>
        public override string GetQueryString() { return _queryString; }

        private byte[] _queryStringBytes;
        /// <summary>在派生类中被重写时，以字节数组的形式返回响应查询字符串。</summary>
        /// <returns>包含响应的字节数组。</returns>
        public override byte[] GetQueryStringRawBytes() { return _queryStringBytes; }

        private string _url;
        /// <summary>返回附加了查询字符串的请求标头中包含的 URL 路径。</summary>
        /// <returns>请求标头的原始 URL 路径。</returns>
        public override string GetRawUrl() { return _url; }

        /// <summary>提供对请求标头的指定成员的访问。</summary>
        /// <returns>客户端的 IP 地址。</returns>
        public override string GetRemoteAddress()
        {
            //_connectionPermission.Assert();
            return Session.ClientEndPoint.Address.ToString();
        }

        /// <summary>提供对请求标头的指定成员的访问。</summary>
        /// <returns>客户端的 HTTP 端口号。</returns>
        public override int GetRemotePort() { return Session.ClientEndPoint.Port; }

        /// <summary>在派生类中被重写时，返回本地服务器的名称。</summary>
        /// <returns>本地服务器的名称。</returns>
        public override string GetServerName()
        {
            string localAddress = GetLocalAddress();
            if (!localAddress.Equals("127.0.0.1") && !localAddress.Equals("::1") && !localAddress.Equals("::ffff:127.0.0.1")) return localAddress;
            return "localhost";
        }

        /// <summary>从与请求关联的服务器变量词典返回单个服务器变量。</summary>
        /// <returns>请求的服务器变量。</returns>
        /// <param name="name">请求的服务器变量的名称。</param>
        public override string GetServerVariable(string name)
        {
            if (String.IsNullOrEmpty(name)) return String.Empty;
            string processUser = string.Empty;
            switch (name.ToUpper())
            {
                case "ALL_RAW":
                    return _allRawHeaders;
                case "SERVER_PROTOCOL":
                    return ResponseHeader.Version;
                case "LOGON_USER":
                    if (GetUserToken() != IntPtr.Zero) processUser = Host.GetProcessUser();
                    return processUser;
                case "AUTH_TYPE":
                    if (GetUserToken() != IntPtr.Zero) processUser = "NTLM";
                    return processUser;
                default:
                    break;
            }
            return String.Empty;
        }

        /// <summary>返回非标准的 HTTP 请求标头值。</summary>
        /// <returns>标头值。</returns>
        /// <param name="name">标头名称。</param>
        public override string GetUnknownRequestHeader(string name)
        {
            int length = _unknownRequestHeaders.Length;
            for (int i = 0; i < length; i++)
            {
                if (string.Compare(name, _unknownRequestHeaders[i][0], StringComparison.OrdinalIgnoreCase) == 0) return _unknownRequestHeaders[i][1];
            }
            return null;
        }

        private string[][] _unknownRequestHeaders;
        /// <summary>获取所有非标准的 HTTP 标头的名称/值对。</summary>
        /// <returns>标头的名称/值对的数组。</returns>
        public override string[][] GetUnknownRequestHeaders() { return _unknownRequestHeaders; }

        private string _path;
        /// <summary>返回请求的 URI 的虚拟路径。</summary>
        /// <returns>请求的 URI 的路径。</returns>
        public override string GetUriPath() { return _path; }

        /// <summary>在派生类中被重写时，返回客户端的模拟标记。</summary>
        /// <returns>表示客户端的模拟标记的值。默认值为 0。</returns>
        public override IntPtr GetUserToken() { return Host.GetProcessToken(); }

        /// <summary>头部是否已发送</summary>
        private bool _headersSent;
        /// <summary>返回一个值，该值指示是否已为当前的请求将 HTTP 响应标头发送到客户端。</summary>
        /// <returns>如果 HTTP 响应标头已发送到客户端，则为 true；否则，为 false。</returns>
        public override bool HeadersSent() { return _headersSent; }

        private bool IsBadPath() { return _path.IndexOfAny(badPathChars) >= 0 || CultureInfo.InvariantCulture.CompareInfo.IndexOf(_path, "..", CompareOptions.Ordinal) >= 0 || CultureInfo.InvariantCulture.CompareInfo.IndexOf(_path, "//", CompareOptions.Ordinal) >= 0; }

        /// <summary>返回一个值，该值指示客户端连接是否仍处于活动状态。</summary>
        /// <returns>如果客户端连接仍处于活动状态，则为 true；否则，为 false。</returns>
        public override bool IsClientConnected()
        {
            //_connectionPermission.Assert();
            return Session.Session.Socket.Connected;
        }

        /// <summary>返回一个值，该值指示是否所有请求数据都可用，以及是否不需要对客户端进行进一步读取。</summary>
        /// <returns>如果所有请求数据都可用，则为 true；否则，为 false。</returns>
        public override bool IsEntireEntityBodyIsPreloaded() { return _contentLength == _preloadedContentLength; }
        #endregion

        #region 处理请求
        [AspNetHostingPermission(SecurityAction.Assert, Level = AspNetHostingPermissionLevel.Medium)]
        public void Process()
        {
            if (!TryParseRequest()) return;

            if (ResponseHeader.Method == "POST" && _contentLength > 0 && _preloadedContentLength < _contentLength) Session.Write100Continue();
            if (!Host.RequireAuthentication || TryNtlmAuthenticate())
            {
                if (_isClientScriptPath)
                    Session.WriteEntireResponseFromFile(Host.PhysicalClientScriptPath + _path.Substring(Host.NormalizedClientScriptPath.Length), false);
                else if (IsRequestForRestrictedDirectory())
                    Session.WriteErrorAndClose(403);
                else if (!ProcessDefaultDocumentRequest())
                {
                    PrepareResponse();
                    HttpRuntime.ProcessRequest(this);
                }
            }
        }

        private bool TryParseRequest()
        {
            Reset();
            ReadAllHeaders();
            if (!Session.IsLocal)
            {
                Session.WriteErrorAndClose(403);
                return false;
            }
            if (_headerBytes == null || _endHeadersOffset < 0 || _headerByteStrings == null || _headerByteStrings.Count == 0)
            {
                Session.WriteErrorAndClose(400);
                return false;
            }
            ParseRequestLine();
            if (IsBadPath())
            {
                Session.WriteErrorAndClose(400);
                return false;
            }
            if (!Host.IsVirtualPathInApp(_path, out _isClientScriptPath))
            {
                Session.WriteErrorAndClose(404);
                return false;
            }
            ParseHeaders();
            ParsePostedContent();
            return true;
        }

        private void Reset()
        {
            _headerBytes = null;
            _startHeadersOffset = 0;
            _endHeadersOffset = 0;
            _headerByteStrings = null;
            _isClientScriptPath = false;
            ResponseHeader.Method = null;
            _url = null;
            ResponseHeader.Version = null;
            _path = null;
            _filePath = null;
            _pathInfo = null;
            _pathTranslated = null;
            _queryString = null;
            _queryStringBytes = null;
            _contentLength = 0;
            _preloadedContentLength = 0;
            _preloadedContent = null;
            _allRawHeaders = null;
            _unknownRequestHeaders = null;
            _knownRequestHeaders = null;
            _specialCaseStaticFileHeaders = false;
        }

        private void ReadAllHeaders()
        {
            _headerBytes = null;
            do
            {
                if (!TryReadAllHeaders()) return;
            }
            while (_endHeadersOffset < 0);
        }

        private void ParseHeaders()
        {
            _knownRequestHeaders = new string[40];
            ArrayList list = new ArrayList();
            for (int i = 1; i < _headerByteStrings.Count; i++)
            {
                string str = ((ByteString)_headerByteStrings[i]).GetString();
                int index = str.IndexOf(':');
                if (index >= 0)
                {
                    string header = str.Substring(0, index).Trim();
                    string str3 = str.Substring(index + 1).Trim();
                    int knownRequestHeaderIndex = HttpWorkerRequest.GetKnownRequestHeaderIndex(header);
                    if (knownRequestHeaderIndex >= 0)
                        _knownRequestHeaders[knownRequestHeaderIndex] = str3;
                    else
                    {
                        list.Add(header);
                        list.Add(str3);
                    }
                }
            }
            int num4 = list.Count / 2;
            _unknownRequestHeaders = new string[num4][];
            int num5 = 0;
            for (int j = 0; j < num4; j++)
            {
                _unknownRequestHeaders[j] = new string[] { (string)list[num5++], (string)list[num5++] };
            }
            if (_headerByteStrings.Count > 1)
                _allRawHeaders = Encoding.UTF8.GetString(_headerBytes, _startHeadersOffset, _endHeadersOffset - _startHeadersOffset);
            else
                _allRawHeaders = string.Empty;
        }

        private void ParsePostedContent()
        {
            _contentLength = 0;
            _preloadedContentLength = 0;
            string s = _knownRequestHeaders[11];
            if (s != null)
            {
                try
                {
                    _contentLength = int.Parse(s, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }
            if (_headerBytes.Length > _endHeadersOffset)
            {
                _preloadedContentLength = _headerBytes.Length - _endHeadersOffset;
                if (_preloadedContentLength > _contentLength) _preloadedContentLength = _contentLength;
                if (_preloadedContentLength > 0)
                {
                    _preloadedContent = new byte[_preloadedContentLength];
                    Buffer.BlockCopy(_headerBytes, _endHeadersOffset, _preloadedContent, 0, _preloadedContentLength);
                }
            }
        }

        private void ParseRequestLine()
        {
            ByteString[] strArray = ((ByteString)_headerByteStrings[0]).Split(' ');
            if (strArray == null || strArray.Length < 2 || strArray.Length > 3)
                Session.WriteErrorAndClose(400);
            else
            {
                ResponseHeader.Method = strArray[0].GetString();
                ByteString str2 = strArray[1];
                _url = str2.GetString();
                if (_url.IndexOf((Char)0xfffd) >= 0) _url = str2.GetString(Encoding.Default);
                if (strArray.Length == 3)
                    ResponseHeader.Version = strArray[2].GetString();
                else
                    ResponseHeader.Version = "HTTP/1.0";
                int index = str2.IndexOf('?');
                if (index > 0)
                    _queryStringBytes = str2.Substring(index + 1).GetBytes();
                else
                    _queryStringBytes = new byte[0];
                index = _url.IndexOf('?');
                if (index > 0)
                {
                    _path = _url.Substring(0, index);
                    _queryString = _url.Substring(index + 1);
                }
                else
                {
                    _path = _url;
                    _queryString = string.Empty;
                }
                if (_path.IndexOf('%') >= 0)
                {
                    _path = HttpUtility.UrlDecode(_path, Encoding.UTF8);
                    index = _url.IndexOf('?');
                    if (index >= 0)
                        _url = _path + _url.Substring(index);
                    else
                        _url = _path;
                }
                int startIndex = _path.LastIndexOf('.');
                int num3 = _path.LastIndexOf('/');
                if (startIndex >= 0 && num3 >= 0 && startIndex < num3)
                {
                    int length = _path.IndexOf('/', startIndex);
                    _filePath = _path.Substring(0, length);
                    _pathInfo = _path.Substring(length);
                }
                else
                {
                    _filePath = _path;
                    _pathInfo = string.Empty;
                }
                _pathTranslated = MapPath(_filePath);
            }
        }

        private void PrepareResponse()
        {
            _headersSent = false;
            ResponseHeader.StatusCode = 200;
            //_responseHeadersBuilder = new StringBuilder();
            _responseBodyBytes = new ArrayList();
        }

        private void SkipAllPostedContent()
        {
            if (_contentLength > 0 && _preloadedContentLength < _contentLength)
            {
                byte[] buffer;
                for (int i = _contentLength - _preloadedContentLength; i > 0; i -= buffer.Length)
                {
                    buffer = Session.ReadRequestBytes(i);
                    if (buffer == null || buffer.Length == 0) return;
                }
            }
        }

        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true), SecurityPermission(SecurityAction.Assert, ControlPrincipal = true)]
        private bool TryNtlmAuthenticate()
        {
            try
            {
                using (var auth = new NtlmAuth())
                {
                    do
                    {
                        string blobString = null;
                        string extraHeaders = _knownRequestHeaders[0x18];
                        if (extraHeaders != null && extraHeaders.StartsWith("NTLM ", StringComparison.Ordinal)) blobString = extraHeaders.Substring(5);
                        var dic = new Dictionary<String, String>();
                        if (blobString != null)
                        {
                            if (!auth.Authenticate(blobString))
                            {
                                Session.WriteErrorAndClose(403);
                                return false;
                            }
                            if (auth.Completed)
                            {
                                if (Host.GetProcessSID() == auth.SID) return true;

                                Session.WriteErrorAndClose(403);
                                return false;
                            }
                            dic["WWW-Authenticate"] = "NTLM " + auth.Blob;
                        }
                        else
                            dic["WWW-Authenticate"] = "NTLM";
                        SkipAllPostedContent();
                        Session.WriteErrorWithExtraHeadersAndKeepAlive(401, dic);
                    }
                    while (TryParseRequest());
                }
            }
            catch
            {
                try
                {
                    Session.WriteErrorAndClose(500);
                }
                catch { }
            }
            return false;
        }

        private bool TryReadAllHeaders()
        {
            byte[] src = Session.ReadRequestBytes(0x8000);
            if (src == null || src.Length == 0) return false;
            if (_headerBytes != null)
            {
                int num = src.Length + _headerBytes.Length;
                if (num > 0x8000) return false;
                byte[] dst = new byte[num];
                Buffer.BlockCopy(_headerBytes, 0, dst, 0, _headerBytes.Length);
                Buffer.BlockCopy(src, 0, dst, _headerBytes.Length, src.Length);
                _headerBytes = dst;
            }
            else
                _headerBytes = src;
            _startHeadersOffset = -1;
            _endHeadersOffset = -1;
            _headerByteStrings = new ArrayList();
            ByteParser parser = new ByteParser(_headerBytes);
            while (true)
            {
                ByteString str = parser.ReadLine();
                if (str == null) break;
                if (_startHeadersOffset < 0) _startHeadersOffset = parser.CurrentOffset;
                if (str.IsEmpty)
                {
                    _endHeadersOffset = parser.CurrentOffset;
                    break;
                }
                _headerByteStrings.Add(str);
            }
            return true;
        }
        #endregion

        #region 响应方法
        /// <summary>终止与客户端的连接。</summary>
        public override void CloseConnection()
        {
            //_connectionPermission.Assert();
            Session.Dispose();
        }

        /// <summary>由运行时使用以通知 <see cref="T:System.Web.HttpWorkerRequest" /> 当前请求的请求处理已完成。</summary>
        public override void EndOfRequest() { }

        /// <summary>将所有挂起的响应数据发送到客户端。</summary>
        /// <param name="finalFlush">如果这将是最后一次刷新响应数据，则为 true；否则为 false。</param>
        public override void FlushResponse(bool finalFlush)
        {
            if (ResponseHeader.StatusCode != 404 || _headersSent || !finalFlush || ResponseHeader.Method != "GET" || !ProcessDirectoryListingRequest())
            {
                //_connectionPermission.Assert();
                if (!_headersSent)
                {
                    Session.WriteHeaders(ResponseHeader.StatusCode, ResponseHeader.Headers);
                    _headersSent = true;
                }
                for (int i = 0; i < _responseBodyBytes.Count; i++)
                {
                    byte[] data = (byte[])_responseBodyBytes[i];
                    Session.WriteBody(data, 0, data.Length);
                }
                _responseBodyBytes = new ArrayList();
                if (finalFlush) Session.Dispose();
            }
        }

        private bool IsRequestForRestrictedDirectory()
        {
            string str = CultureInfo.InvariantCulture.TextInfo.ToLower(_path);
            if (Host.VirtualPath != "/") str = str.Substring(Host.VirtualPath.Length);
            foreach (string str2 in restrictedDirs)
            {
                if (str.StartsWith(str2, StringComparison.Ordinal) && (str.Length == str2.Length || str[str2.Length] == '/')) return true;
            }
            return false;
        }

        /// <summary>返回与指定虚拟路径相对应的物理路径。</summary>
        /// <returns>与 <paramref name="path" /> 参数中指定的虚拟路径相对应的物理路径。</returns>
        /// <param name="path">虚拟路径。</param>
        public override string MapPath(string path)
        {
            string physicalPath = string.Empty;
            bool isClientScriptPath = false;
            if (path == null || path.Length == 0 || path.Equals("/"))
            {
                if (Host.VirtualPath == "/")
                    physicalPath = Host.PhysicalPath;
                else
                    physicalPath = Environment.SystemDirectory;
            }
            else if (Host.IsVirtualPathAppPath(path))
                physicalPath = Host.PhysicalPath;
            else if (Host.IsVirtualPathInApp(path, out isClientScriptPath))
            {
                if (isClientScriptPath)
                    physicalPath = Host.PhysicalClientScriptPath + path.Substring(Host.NormalizedClientScriptPath.Length);
                else
                    physicalPath = Host.PhysicalPath + path.Substring(Host.NormalizedVirtualPath.Length);
            }
            else if (path.StartsWith("/", StringComparison.Ordinal))
                physicalPath = Host.PhysicalPath + path.Substring(1);
            else
                physicalPath = Host.PhysicalPath + path;
            physicalPath = physicalPath.Replace('/', '\\');
            if (physicalPath.EndsWith(@"\", StringComparison.Ordinal) && !physicalPath.EndsWith(@":\", StringComparison.Ordinal)) physicalPath = physicalPath.Substring(0, physicalPath.Length - 1);
            return physicalPath;
        }

        private bool ProcessDefaultDocumentRequest()
        {
            if (ResponseHeader.Method == "GET")
            {
                string path = _pathTranslated;
                if (_pathInfo.Length > 0) path = MapPath(_path);
                if (!Directory.Exists(path)) return false;
                if (!_path.EndsWith("/", StringComparison.Ordinal))
                {
                    string str2 = _path + "/";
                    //string extraHeaders = "Location: " + UrlEncodeRedirect(str2) + "\r\n";
                    ResponseHeader.Location = UrlEncodeRedirect(str2);
                    string body = "<html><head><title>Object moved</title></head><body>\r\n<h2>Object moved to <a href='" + str2 + "'>here</a>.</h2>\r\n</body></html>\r\n";
                    Session.WriteEntireResponseFromString(0x12e, ResponseHeader.Headers, body, false);
                    return true;
                }
                foreach (string str5 in defaultFileNames)
                {
                    string str6 = path + @"\" + str5;
                    if (File.Exists(str6))
                    {
                        _path = _path + str5;
                        _filePath = _path;
                        _url = (_queryString != null) ? (_path + "?" + _queryString) : _path;
                        _pathTranslated = str6;
                        return false;
                    }
                }
            }
            return false;
        }

        private bool ProcessDirectoryListingRequest()
        {
            if (ResponseHeader.Method != "GET") return false;
            string path = _pathTranslated;
            if (_pathInfo.Length > 0) path = MapPath(_path);
            if (!Directory.Exists(path)) return false;
            if (Host.DisableDirectoryListing) return false;
            FileSystemInfo[] elements = null;
            try
            {
                elements = new DirectoryInfo(path).GetFileSystemInfos();
            }
            catch { }
            string str2 = null;
            if (_path.Length > 1)
            {
                int length = _path.LastIndexOf('/', _path.Length - 2);
                str2 = (length > 0) ? _path.Substring(0, length) : "/";
                if (!Host.IsVirtualPathInApp(str2)) str2 = null;
            }
            var header = new HttpHeader();
            header.ContentType = "text/html; charset=utf-8";
            Session.WriteEntireResponseFromString(200, header.Headers, Messages.FormatDirectoryListing(_path, str2, elements), false);
            //Session.WriteEntireResponseFromString(200, "Content-type: text/html; charset=utf-8\r\n", Messages.FormatDirectoryListing(_path, str2, elements), false);
            return true;
        }

        /// <summary>读取客户端的请求数据（在尚未预加载时）。</summary>
        /// <returns>读取的字节数。</returns>
        /// <param name="buffer">将数据读入的字节数组。</param>
        /// <param name="size">最多读取的字节数。</param>
        public override int ReadEntityBody(byte[] buffer, int size)
        {
            int count = 0;
            //_connectionPermission.Assert();
            byte[] src = Session.ReadRequestBytes(size);
            if (src != null && src.Length > 0)
            {
                count = src.Length;
                Buffer.BlockCopy(src, 0, buffer, 0, count);
            }
            return count;
        }

        /// <summary>将 Content-Length HTTP 标头添加到小于或等于 2 GB 的消息正文的响应。</summary>
        /// <param name="contentLength">响应的长度（以字节为单位）。</param>
        public override void SendCalculatedContentLength(int contentLength)
        {
            if (!_headersSent) ResponseHeader.ContentLength = contentLength;
        }

        /// <summary>将标准 HTTP 标头添加到响应。</summary>
        /// <param name="index">标头索引。例如 <see cref="F:System.Web.HttpWorkerRequest.HeaderContentLength" />。</param>
        /// <param name="value">标头的值。</param>
        public override void SendKnownResponseHeader(int index, string value)
        {
            if (!_headersSent)
            {
                switch (index)
                {
                    case 1:
                    case 2:
                    case 0x1a:
                        return;

                    case 0x12:
                    case 0x13:
                        if (!_specialCaseStaticFileHeaders) break;
                        return;

                    case 20:
                        if (!(value == "bytes")) break;
                        _specialCaseStaticFileHeaders = true;
                        return;
                }
                ResponseHeader.Headers[HttpWorkerRequest.GetKnownResponseHeaderName(index)] = value;
                //_responseHeadersBuilder.Append(HttpWorkerRequest.GetKnownResponseHeaderName(index));
                //_responseHeadersBuilder.Append(": ");
                //_responseHeadersBuilder.Append(value);
                //_responseHeadersBuilder.Append("\r\n");
            }
        }

        /// <summary>将指定文件的内容添加到响应并指定文件中的起始位置和要发送的字节数。</summary>
        /// <param name="handle">要发送的文件的句柄。</param>
        /// <param name="offset">文件中的起始位置。</param>
        /// <param name="length">要发送的字节数。</param>
        public override void SendResponseFromFile(IntPtr handle, long offset, long length)
        {
            if (length != 0)
            {
                FileStream f = null;
                try
                {
                    SafeFileHandle handle2 = new SafeFileHandle(handle, false);
                    f = new FileStream(handle2, FileAccess.Read);
                    SendResponseFromFileStream(f, offset, length);
                }
                finally
                {
                    if (f != null)
                    {
                        f.Close();
                        f = null;
                    }
                }
            }
        }

        /// <summary>将指定文件的内容添加到响应并指定文件中的起始位置和要发送的字节数。</summary>
        /// <param name="filename">要发送的文件的名称。</param>
        /// <param name="offset">文件中的起始位置。</param>
        /// <param name="length">要发送的字节数。</param>
        public override void SendResponseFromFile(string filename, long offset, long length)
        {
            if (length != 0)
            {
                FileStream f = null;
                try
                {
                    f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    SendResponseFromFileStream(f, offset, length);
                }
                finally
                {
                    if (f != null) f.Close();
                }
            }
        }

        /// <summary>将指定文件的内容添加到响应并指定文件中的起始位置和要发送的字节数。</summary>
        /// <param name="f">要发送的文件的名称。</param>
        /// <param name="offset">文件中的起始位置。</param>
        /// <param name="length">要发送的字节数。</param>
        private void SendResponseFromFileStream(FileStream f, long offset, long length)
        {
            long num = f.Length;
            if (length == -1) length = num - offset;
            if (length != 0 && offset >= 0 && length <= num - offset)
            {
                if (offset > 0) f.Seek(offset, SeekOrigin.Begin);
                if (length <= 0x10000)
                {
                    byte[] buffer = new byte[(int)length];
                    int num2 = f.Read(buffer, 0, (int)length);
                    SendResponseFromMemory(buffer, num2);
                }
                else
                {
                    byte[] buffer2 = new byte[0x10000];
                    int num3 = (int)length;
                    while (num3 > 0)
                    {
                        int count = (num3 < 0x10000) ? num3 : 0x10000;
                        int num5 = f.Read(buffer2, 0, count);
                        SendResponseFromMemory(buffer2, num5);
                        num3 -= num5;
                        if (num3 > 0 && num5 > 0) FlushResponse(false);
                    }
                }
            }
        }

        /// <summary>将字节数组中指定数目的字节添加到响应。</summary>
        /// <param name="data">要发送的字节数组。</param>
        /// <param name="length">要发送的字节数（从第一个字节开始）。</param>
        public override void SendResponseFromMemory(byte[] data, int length)
        {
            if (length > 0)
            {
                byte[] dst = new byte[length];
                Buffer.BlockCopy(data, 0, dst, 0, length);
                _responseBodyBytes.Add(dst);
            }
        }

        /// <summary>指定响应的 HTTP 状态代码和状态说明，例如 SendStatus(200, "Ok")。</summary>
        /// <param name="statusCode">要发送的状态代码</param>
        /// <param name="statusDescription">要发送的状态说明。</param>
        public override void SendStatus(int statusCode, string statusDescription) { ResponseHeader.StatusCode = statusCode; }

        /// <summary>将非标准 HTTP 标头添加到响应。</summary>
        /// <param name="name">要发送的标头的名称。</param>
        /// <param name="value">标头的值。</param>
        public override void SendUnknownResponseHeader(string name, string value)
        {
            if (!_headersSent)
            {
                ResponseHeader.Headers[name] = value;
                //_responseHeadersBuilder.Append(name);
                //_responseHeadersBuilder.Append(": ");
                //_responseHeadersBuilder.Append(value);
                //_responseHeadersBuilder.Append("\r\n");
            }
        }
        #endregion

        #region 辅助
        private static string UrlEncodeRedirect(string path)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(path);
            int length = bytes.Length;
            int num2 = 0;
            for (int i = 0; i < length; i++)
            {
                if ((bytes[i] & 0x80) != 0) num2++;
            }
            if (num2 > 0)
            {
                byte[] buffer2 = new byte[length + num2 * 2];
                int num4 = 0;
                for (int j = 0; j < length; j++)
                {
                    byte num6 = bytes[j];
                    if ((num6 & 0x80) == 0)
                        buffer2[num4++] = num6;
                    else
                    {
                        buffer2[num4++] = 0x25;
                        buffer2[num4++] = (byte)IntToHex[num6 >> 4 & 15];
                        buffer2[num4++] = (byte)IntToHex[num6 & 15];
                    }
                }
                path = Encoding.ASCII.GetString(buffer2);
            }
            if (path.IndexOf(' ') >= 0) path = path.Replace(" ", "%20");
            return path;
        }
        #endregion
    }
}