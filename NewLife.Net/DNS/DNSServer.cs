using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Net.Common;
using NewLife.Net.Sockets;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace NewLife.Net.DNS
{
    /// <summary>DNS服务器</summary>
    public class DNSServer : NetServer
    {
        #region 属性
        private String _DomainName;
        /// <summary>域名</summary>
        public String DomainName { get { return _DomainName ?? "dns.nnhy.org"; } set { _DomainName = value; } }

        private List<NetUri> _Parents;
        /// <summary>上级DNS地址</summary>
        public List<NetUri> Parents
        {
            get
            {
                if (_Parents == null)
                {
                    var list = new List<NetUri>();
                    foreach (var item in NetHelper.GetDns())
                    {
                        if (item.IsAny())
                        {
                            WriteLog("取得的本地DNS[{0}]有误，任意地址不能作为父级DNS地址。", item);
                            continue;
                        }
                        var uri = new NetUri(ProtocolType.Udp, item, 53);
                        WriteLog("使用本地地址作为父级DNS：{0}", uri);
                        list.Add(uri);
                    }
                    list.Add(new NetUri("tcp://8.8.8.8:53"));
                    list.Add(new NetUri("udp://4.4.4.4:53"));

                    _Parents = list;
                }
                return _Parents;
            }
            set { _Parents = value; }
        }

        /// <summary>上级DNS地址，多个地址以逗号隔开</summary>
        public String Parent
        {
            get
            {
                var ps = Parents;
                if (ps == null || ps.Count < 1) return null;

                var sb = new StringBuilder();
                foreach (var item in ps)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(item);
                }
                return sb.ToString();
            }
            set
            {
                var ps = Parents;
                var list = new HashSet<String>(ps.Select(p => p.ToString()), StringComparer.OrdinalIgnoreCase);
                ps.Clear();

                if (value.IsNullOrWhiteSpace()) return;

                var ss = value.Split(",");
                if (ss == null || ss.Length < 1) return;

                foreach (var item in ss)
                {
                    var uri = new NetUri(item);
                    if (uri.Port <= 0) uri.Port = 53;
                    if (!list.Contains(uri.ToString()))
                    {
                        if (uri.Address.IsAny())
                        {
                            WriteLog("配置的父级DNS[{0}]有误，任意地址不能作为父级DNS地址。", uri);
                            continue;
                        }
                        ps.Add(uri);
                        list.Add(uri.ToString());
                    }
                }
            }
        }
        #endregion

        #region 构造
        /// <summary>实例化一个DNS服务器</summary>
        public DNSServer() { Port = 53; }
        #endregion

        #region 方法
        DictionaryCache<String, DNSEntity> cache = new DictionaryCache<string, DNSEntity>() { Expriod = 600, Asynchronous = true };

        /// <summary>接收处理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            var session = e.Session;
            Boolean isTcp = session.ProtocolType == ProtocolType.Tcp;

            // 解析
            var request = DNSEntity.Read(e.GetStream(), isTcp);

            var response = Request(session, request);
            if (response != null)
            {
                response.Header.ID = request.Header.ID;
                Response(session, request, response);
            }

            session.Dispose();
        }

        /// <summary>处理请求</summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual DNSEntity Request(ISocketSession session, DNSEntity request)
        {
            Boolean isTcp = session.ProtocolType == ProtocolType.Tcp;

            // 处理，修改
            WriteDNSLog("{0}://{1} 请求 {2}", session.ProtocolType, session.RemoteEndPoint, request);

            // 请求事件，如果第二参数有值，则直接返回
            if (OnRequest != null)
            {
                var e = new DNSEventArgs();
                e.Request = request;
                OnRequest(this, e);
                if (e.Response != null) return e.Response;
            }

            // 如果是PTR请求
            if (request.Type == DNSQueryType.PTR)
            {
                var ptr = RequestPTR(request);
                if (ptr != null) return ptr;
            }

            // 读取缓存
            var rs = cache.GetItem<DNSEntity>(request.ToString(), request, GetDNS, false);

            // 返回给客户端
            if (rs != null)
            {
                //String file = String.Format("dns_{0:MMddHHmmss}.bin", DateTime.Now);
                //File.WriteAllBytes(file, entity2.GetStream().ReadBytes());

                // 如果是PTR请求
                if (request.Type == DNSQueryType.PTR && rs.Type == DNSQueryType.PTR)
                {
                    var ptr = request.Questions[0] as DNS_PTR;
                    var ptr2 = rs.GetAnswer() as DNS_PTR;
                    if (ptr2 != null)
                    {
                        ptr2.Name = ptr.Name;
                        ptr2.DomainName = DomainName;
                    }
                    if (rs.Answers != null && rs.Answers.Length > 0)
                    {
                        foreach (var item in rs.Answers)
                        {
                            if (item.Type == DNSQueryType.PTR) item.Name = ptr.Name;
                        }
                    }
                }
            }

            return rs;
        }

        /// <summary>处理PTR请求</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual DNSEntity RequestPTR(DNSEntity request)
        {
            var ptr = request.Questions[0] as DNS_PTR;
            // 对本地的请求马上返回
            var addr = ptr.Address;
            if (addr != null && (IPAddress.IsLoopback(addr) || NetHelper.GetIPs().Any(ip => ip + "" == addr + "")))
            {
                var ptr2 = new DNS_PTR();
                ptr2.Name = ptr.Name;
                ptr2.DomainName = DomainName;

                var rs = new DNSEntity();
                rs.Questions = request.Questions;
                rs.Answers = new DNSRecord[] { ptr2 };

                rs.Header.ID = request.Header.ID;
                return rs;
            }
            return null;
        }

        /// <summary>处理响应</summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        protected virtual void Response(ISocketSession session, DNSEntity request, DNSEntity response)
        {
            Boolean isTcp = session.ProtocolType == ProtocolType.Tcp;

            if (OnResponse != null)
            {
                var e = new DNSEventArgs { Request = request, Response = response, Session = session };
                OnResponse(this, e);
            }

            session.Send(response.GetStream(isTcp));
        }

        DNSEntity GetDNS(String key, DNSEntity request)
        {
            // 请求父级代理
            NetUri parent = null;
            Byte[] data = null;
            ISocketSession session = null;
            foreach (var item in Parents)
            {
                session = NetService.CreateSession(item);
                parent = item;
                // 如果是PTR请求
                if (request.Type == DNSQueryType.PTR)
                {
                    // 复制一份，防止修改外部
                    request = new DNSEntity().CloneFrom(request);

                    var ptr = request.GetAnswer(true) as DNS_PTR;
                    if (ptr != null) ptr.Address = parent.Address;
                }

                try
                {
                    session.Send(request.GetStream(item.ProtocolType == ProtocolType.Tcp));
                    data = session.Receive();

                    if (data != null && data.Length > 0) break;
                }
                catch { }
            }
            if (data == null || data.Length < 1) return null;

            DNSEntity response = null;
            try
            {
                // 解析父级代理返回的数据
                response = DNSEntity.Read(data, parent.ProtocolType == ProtocolType.Tcp);

                // 处理，修改
                WriteDNSLog("{0} 返回 {1}", parent, response);
            }
            catch (Exception ex)
            {
                String file = String.Format("dns_{0:MMddHHmmss}.bin", DateTime.Now);
                XTrace.WriteLine("解析父级代理返回数据出错！数据保存于" + file + "。" + ex.Message);
                File.WriteAllBytes(file, data);
            }

            if (OnNew != null)
            {
                var e = new DNSEventArgs { Request = request, Response = response, Session = session };
                OnNew(this, e);
            }

            return response;
        }
        #endregion

        #region 事件
        /// <summary>请求时触发。</summary>
        public event EventHandler<DNSEventArgs> OnRequest;

        /// <summary>响应时触发。</summary>
        public event EventHandler<DNSEventArgs> OnResponse;

        /// <summary>取得新DNS时触发。</summary>
        public event EventHandler<DNSEventArgs> OnNew;
        #endregion

        #region 写日志
        static TextFileLog log = TextFileLog.Create("DNSLog");
        [Conditional("DEBUG")]
        void WriteDNSLog(String format, params Object[] args)
        {
            log.WriteLine(format, args);
        }
        #endregion
    }

    /// <summary>DNS事件参数</summary>
    public class DNSEventArgs : EventArgs
    {
        private DNSEntity _Request;
        /// <summary>请求</summary>
        public DNSEntity Request { get { return _Request; } set { _Request = value; } }

        private DNSEntity _Response;
        /// <summary>响应</summary>
        public DNSEntity Response { get { return _Response; } set { _Response = value; } }

        private ISocketSession _Session;
        /// <summary>网络会话</summary>
        public ISocketSession Session { get { return _Session; } set { _Session = value; } }
    }
}