using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Net.DNS
{
    /// <summary>DNS服务器</summary>
    public class DNSServer : NetServer
    {
        #region 属性
        /// <summary>域名</summary>
        public String DomainName { get; set; }

        /// <summary>上级DNS地址</summary>
        public List<NetUri> Parents { get; set; } = new List<NetUri>();
        #endregion

        #region 构造
        /// <summary>实例化一个DNS服务器</summary>
        public DNSServer()
        {
            //Name = "DNS";
            Port = 53;

            DomainName = "dns.NewLifeX.com";

            SocketLog = Logger.Null;
            SessionLog = Logger.Null;
        }
        #endregion

        #region 父级DNS
        /// <summary>获取本机DNS列表</summary>
        /// <returns></returns>
        public virtual List<NetUri> GetLocalDNS()
        {
            var list = new List<NetUri>();
            foreach (var item in NetHelper.GetDns())
            {
                if (!item.IsIPv4()) continue;

                if (item.IsAny())
                {
                    WriteLog("取得的本地DNS[{0}]有误，任意地址不能作为父级DNS地址。", item);
                    continue;
                }
                var uri = new NetUri(NetType.Udp, item, 53);
                WriteLog("使用本地地址作为父级DNS：{0}", uri);
                list.Add(uri);
            }

            return list;
        }

        /// <summary>设置父级DNS</summary>
        /// <param name="parents"></param>
        public virtual void SetParents(String parents)
        {
            var ss = parents.Split(",");
            if (ss == null || ss.Length < 1) return;

            var ps = Parents;
            var list = new HashSet<String>(ps.Select(p => p.ToString()), StringComparer.OrdinalIgnoreCase);
            //ps.Clear();

            for (var i = ss.Length - 1; i >= 0; i--)
            {
                try
                {
                    var uri = new NetUri(ss[i]);
                    if (uri.Port <= 0) uri.Port = 53;
                    if (!list.Contains(uri.ToString()))
                    {
                        if (uri.Address.IsAny())
                        {
                            WriteLog("配置的父级DNS[{0}]有误，任意地址不能作为父级DNS地址。", uri);
                            continue;
                        }
                        ps.Insert(0, uri);
                        list.Add(uri.ToString());
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("配置的父级DNS[{0}]有误，{1}", ss[i], ex.Message);
                }
            }
        }

        private List<DNSClient> _Clients;
        #endregion

        #region 方法
        /// <summary>启动服务</summary>
        protected override void OnStart()
        {
            // 如果没有设置父级DNS，则使用本地DNS
            var ps = Parents;
            if (ps.Count == 0) ps.AddRange(GetLocalDNS());

            base.OnStart();

            // 准备连接
            _Clients = new List<DNSClient>();
            foreach (var item in Parents.ToArray())
            {
                var nc = new DNSClient(item);
                TaskEx.Run(() =>
                {
                    if (nc.Open())
                    {
                        WriteLog("已连接父级DNS：{0}", nc.Client.Remote);
                        lock (_Clients) { _Clients.Add(nc); }
                    }
                });
            }
        }

        /// <summary>停止服务</summary>
        protected override void OnStop()
        {
            base.OnStop();

            _Clients.TryDispose();
            _Clients = null;
        }

        DictionaryCache<String, DNSEntity> cache = new DictionaryCache<String, DNSEntity>() { Expire = 600/*, Asynchronous = true, CacheDefault = false*/ };

        /// <summary>接收处理</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        protected override void OnReceive(INetSession session, Stream stream)
        {
            var isTcp = session.Session.Local.IsTcp;

            // 解析
            var request = DNSEntity.Read(stream, isTcp);

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
        protected virtual DNSEntity Request(INetSession session, DNSEntity request)
        {
            var local = session.Session.Local;
            var isTcp = local.IsTcp;

            // 处理，修改
            WriteLog("{0} 请求 {1}", session.Session.Remote, request);

            // 请求事件，如果第二参数有值，则直接返回
            // 结合数据库缓存，可以在这里进行返回
            if (OnRequest != null)
            {
                var e = new DNSEventArgs
                {
                    Request = request
                };
                OnRequest(this, e);
                if (e.Response != null) return e.Response;
            }

            // 如果是PTR请求
            var rq = request.Questions[0];
            if (rq.Type == DNSQueryType.PTR)
            {
                var ptr = RequestPTR(request);
                if (ptr != null) return ptr;
            }

            // 读取缓存
            //var rs = cache.GetItem(request.ToString(), k => GetDNS(k, request));
            var key = request.ToString();
            var rs = cache[key];
            if (rs == null) cache[key] = rs = GetDNS(key, request);

            // 返回给客户端
            if (rs != null)
            {
                // 如果是PTR请求
                if (rq.Type == DNSQueryType.PTR && rs.Questions[0].Type == DNSQueryType.PTR)
                {
                    var ptr = rq as DNS_PTR;
                    if (rs.GetAnswer() is DNS_PTR ptr2)
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
            var rq = request.Questions[0];
            var ptr = rq as DNS_PTR;
            if (ptr == null) ptr = new DNS_PTR { Name = rq.Name };
            // 对本地的请求马上返回
            var addr = ptr.Address;
            if (addr != null && addr.IsLocal())
            {
                var ptr2 = new DNS_PTR
                {
                    Name = ptr.Name,
                    DomainName = DomainName
                };

                var rs = new DNSEntity
                {
                    Questions = request.Questions,
                    Answers = new DNSRecord[] { ptr2 }
                };

                rs.Header.ID = request.Header.ID;
                return rs;
            }
            return null;
        }

        /// <summary>处理响应</summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        protected virtual void Response(INetSession session, DNSEntity request, DNSEntity response)
        {
            var ss = session?.Session;
            if (ss == null) return;

            var isTcp = ss.Local.IsTcp;

            if (OnResponse != null)
            {
                var e = new DNSEventArgs { Request = request, Response = response, Session = ss };
                OnResponse(this, e);
            }

            session?.Send(response.GetStream(isTcp));
        }

        DNSEntity GetDNS(String key, DNSEntity request)
        {
            // 批量请求父级代理
            var dic = DNSClient.QueryAll(_Clients, request);
            if (dic.Count == 0) return null;

            DNSEntity rs = null;
            foreach (var item in dic)
            {
                rs = item.Value;
                var nc = item.Key.Client;

                WriteLog("{0} GetDNS {1}", nc.Remote, rs);

                if (OnNew != null)
                {
                    var e = new DNSEventArgs { Request = request, Response = item.Value, Session = nc };
                    OnNew(this, e);
                }
            }

            return rs;
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
    }

    /// <summary>DNS事件参数</summary>
    public class DNSEventArgs : EventArgs
    {
        /// <summary>请求</summary>
        public DNSEntity Request { get; set; }

        /// <summary>响应</summary>
        public DNSEntity Response { get; set; }

        /// <summary>网络会话</summary>
        public ISocketRemote Session { get; set; }
    }
}