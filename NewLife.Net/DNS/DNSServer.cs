using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NewLife.Collections;
using NewLife.Log;

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

            for (int i = ss.Length - 1; i >= 0; i--)
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
        #endregion

        #region 方法
        /// <summary>启动服务</summary>
        protected override void OnStart()
        {
            // 如果没有设置父级DNS，则使用本地DNS
            var ps = Parents;
            if (ps.Count == 0) ps.AddRange(GetLocalDNS());

            WriteLog("父级DNS[{0}]：", ps.Count);
            foreach (var item in ps)
            {
                WriteLog("\t{0}", item);
            }

            base.OnStart();
        }

        DictionaryCache<String, DNSEntity> cache = new DictionaryCache<string, DNSEntity>() { Expire = 600, Asynchronous = true, CacheDefault = false };

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
            WriteLog("{0} 请求 {1}", local.Type, request);

            // 请求事件，如果第二参数有值，则直接返回
            // 结合数据库缓存，可以在这里进行返回
            if (OnRequest != null)
            {
                var e = new DNSEventArgs();
                e.Request = request;
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
            var rs = cache.GetItem(request.ToString(), k => GetDNS(k, request));

            // 返回给客户端
            if (rs != null)
            {
                // 如果是PTR请求
                if (rq.Type == DNSQueryType.PTR && rs.Questions[0].Type == DNSQueryType.PTR)
                {
                    var ptr = rq as DNS_PTR;
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
            var rq = request.Questions[0];
            var ptr = rq as DNS_PTR;
            if (ptr == null) ptr = new DNS_PTR { Name = rq.Name };
            // 对本地的请求马上返回
            var addr = ptr.Address;
            if (addr != null && addr.IsLocal())
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
        protected virtual void Response(INetSession session, DNSEntity request, DNSEntity response)
        {
            var isTcp = session.Session.Local.IsTcp;

            if (OnResponse != null)
            {
                var e = new DNSEventArgs { Request = request, Response = response, Session = session.Session };
                OnResponse(this, e);
            }

            if (session != null && !session.Disposed) session.Send(response.GetStream(isTcp));
        }

        private IDictionary<String, ISocketClient> _Clients = new Dictionary<String, ISocketClient>();

        DNSEntity GetDNS(String key, DNSEntity request)
        {
            // 准备连接
            if (_Clients == null)
            {
                var dic = new Dictionary<String, ISocketClient>();
                foreach (var item in Parents.ToArray())
                {
                    var nc = item.CreateRemote();
                    nc.Timeout = 1000;
                    dic[item + ""] = nc;
                }

                _Clients = dic;
            }

            // 请求父级代理
            NetUri parent = null;
            Byte[] data = null;
            ISocketClient client = null;

            foreach (var item in _Clients)
            {
                WriteLog("GetDNS key={0} {1}", key, item.Key);

                client = item.Value;
                var remote = client.Remote;
                // 如果是PTR请求
                if (request.Questions[0].Type == DNSQueryType.PTR)
                {
                    // 复制一份，防止修改外部
                    request = new DNSEntity().CloneFrom(request);

                    var ptr = request.GetAnswer(true) as DNS_PTR;
                    if (ptr != null) ptr.Address = remote.Address;
                }

                // 发送DNS请求
                try
                {
                    client.Send(request.GetStream(remote.IsTcp));
                    data = client.Receive();

                    if (data != null && data.Length > 0)
                    {
                        parent = remote;
                        break;
                    }
                }
                catch { }
            }
            if (data == null || data.Length < 1) return null;

            //// 调到第一位
            //if (Parents.Count > 1 && Parents[0] != parent)
            //{
            //    lock (Parents)
            //    {
            //        if (Parents.Count > 1 && Parents[0] != parent)
            //        {
            //            Parents.Remove(parent);
            //            Parents.Insert(0, parent);
            //        }
            //    }
            //}

            DNSEntity response = null;
            try
            {
                // 解析父级代理返回的数据
                response = DNSEntity.Read(data, parent.IsTcp);

                // 处理，修改
                WriteLog("{0} 返回 {1}", parent, response);
            }
            catch (Exception ex)
            {
                String file = String.Format("dns_{0:MMddHHmmss}.bin", DateTime.Now);
                XTrace.WriteLine("解析父级代理返回数据出错！数据保存于" + file + "。" + ex.Message);
                File.WriteAllBytes(file, data);
            }

            if (OnNew != null)
            {
                var e = new DNSEventArgs { Request = request, Response = response, Session = client };
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