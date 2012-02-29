using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NewLife.Collections;
using NewLife.Net.Sockets;
using System.Text;
using NewLife.Reflection;

namespace NewLife.Net.DNS
{
    /// <summary>DNS服务器</summary>
    public class DNSServer : NetServer
    {
        #region 属性
        private String _DomainName;
        /// <summary>域名</summary>
        public String DomainName { get { return _DomainName ?? this.GetType().FullName; } set { _DomainName = value; } }

        private Dictionary<IPEndPoint, ProtocolType> _Parents;
        /// <summary>上级DNS地址</summary>
        public Dictionary<IPEndPoint, ProtocolType> Parents { get { return _Parents ?? (_Parents = new Dictionary<IPEndPoint, ProtocolType>()); } set { _Parents = value; } }

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
                    sb.AppendFormat("{0}://{1}", item.Value, item.Key);
                }
                return sb.ToString();
            }
            set
            {
                var ps = Parents;
                ps.Clear();

                if (value.IsNullOrWhiteSpace()) return;

                var dic = value.SplitAsDictionary("://", ",");
                if (dic == null || dic.Count < 1) return;

                foreach (var item in dic)
                {
                    var ep = NetHelper.ParseEndPoint(item.Value, 53);
                    var pt = TypeX.ChangeType<ProtocolType>(item.Key);
                    ps[ep] = pt;
                }
            }
        }
        #endregion

        #region 构造
        /// <summary>实例化一个DNS服务器</summary>
        public DNSServer() { Port = 53; }
        #endregion

        #region 方法
        DictionaryCache<String, DNSEntity> cache = new DictionaryCache<string, DNSEntity>();

        /// <summary>接收处理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            var session = e.Session;
            Boolean isTcp = session.ProtocolType == ProtocolType.Tcp;

            // 解析
            var entity = DNSEntity.Read(e.GetStream(), isTcp);

            // 处理，修改
            WriteLog("{0}://{1} 请求 {2}", session.ProtocolType, e.RemoteEndPoint, entity);

            // 读取缓存
            var entity2 = cache.GetItem<DNSEntity>(entity.ToString(), entity, GetDNS, false);
            //var entity2 = GetDNS(null, entity);

            // 返回给客户端
            if (entity2 != null)
            {
                // 如果是PTR请求
                if (entity is DNS_PTR && entity2 is DNS_PTR)
                {
                    var ptr = entity as DNS_PTR;
                    var ptr2 = entity2 as DNS_PTR;
                    ptr2.Name = ptr.Name;
                    ptr2.DomainName = DomainName;
                    if (ptr2.Answers != null && ptr2.Answers.Length > 0)
                    {
                        foreach (var item in ptr2.Answers)
                        {
                            if (item.Type == DNSQueryType.PTR) item.Name = ptr.Name;
                        }
                    }
                }
                entity2.Header.ID = entity.Header.ID;
                //session.Send(entity2.GetStream(isTcp), e.RemoteEndPoint);
                session.Send(entity2.GetStream(isTcp));
            }
            //session.Disconnect();
            session.Dispose();
        }

        DNSEntity GetDNS(String key, DNSEntity entity)
        {
            // 请求父级代理
            IPEndPoint ep = null;
            ProtocolType pt = ProtocolType.Tcp;
            Boolean isTcp = false;
            Byte[] data = null;
            foreach (var item in Parents)
            {
                isTcp = item.Value == ProtocolType.Tcp;
                //var client = NetService.Resolve<ISocketClient>(item.Value);
                var session = NetService.CreateSession(new Common.NetUri(item.Value, item.Key));
                ep = item.Key;
                pt = item.Value;
                // 如果是PTR请求
                if (entity is DNS_PTR)
                {
                    // 复制一份，防止修改外部
                    entity = new DNS_PTR().CloneFrom(entity);

                    var ptr = entity as DNS_PTR;
                    ptr.Address = ep.Address;
                }

                try
                {
                    //client.Connect(ep);
                    //client.Send(entity.GetStream(isTcp), ep);
                    //client.CreateSession(ep).Send(entity.GetStream(isTcp));
                    session.Send(entity.GetStream(isTcp));
                    data = session.Receive();

                    if (data != null && data.Length > 0) break;
                }
                catch { }
            }
            if (data == null || data.Length < 1) return null;

            DNSEntity entity2 = null;
            try
            {
                // 解析父级代理返回的数据
                entity2 = DNSEntity.Read(data, isTcp);

                // 处理，修改
                WriteLog("{0}://{1} 返回 {2}", pt, ep, entity2);
            }
            catch (Exception ex)
            {
                String file = String.Format("dns_{0:MMddHHmmss}.bin", DateTime.Now);
                WriteLog("解析父级代理返回数据出错！数据保存于" + file + "。" + ex.Message);
                File.WriteAllBytes(file, data);
            }

            return entity2;
        }
        #endregion
    }
}