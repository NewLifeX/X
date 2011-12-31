using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Collections;
using System.Net.Sockets;
using NewLife.Linq;

namespace NewLife.Net.Protocols.DNS
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
        #endregion

        #region 构造

        #endregion

        #region 方法
        DictionaryCache<String, DNSEntity> cache = new DictionaryCache<string, DNSEntity>();

        /// <summary>接收处理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            var session = e.Socket as ISocketSession;
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
                session.Send(entity2.GetStream(isTcp), e.RemoteEndPoint);
            }
            session.Disconnect();
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
                var client = NetService.Resolve<ISocketClient>(item.Value);
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
                    client.Connect(ep);
                    client.Send(entity.GetStream(isTcp), ep);
                    data = client.Receive();

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
                WriteLog("解析父级代理返回数据出错！数据保存着" + file + "。" + ex.Message);
                File.WriteAllBytes(file, data);
            }

            return entity2;
        }
        #endregion
    }
}