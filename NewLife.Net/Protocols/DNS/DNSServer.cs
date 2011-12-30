using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Collections;
using System.Net.Sockets;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>DNS服务器</summary>
    public class DNSServer : NetServer
    {
        #region 属性
        private Dictionary<ProtocolType, IPEndPoint> _Parents;
        /// <summary>上级DNS地址</summary>
        public Dictionary<ProtocolType, IPEndPoint> Parents { get { return _Parents ?? (_Parents = new Dictionary<ProtocolType, IPEndPoint>()); } set { _Parents = value; } }
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
            WriteLog("{0} 请求 {1}", e.RemoteEndPoint, entity);

            // 读取缓存
            entity = cache.GetItem<DNSEntity>(entity.ToString(), entity, GetDNS, false);

            // 返回给客户端
            if (entity != null) session.Send(entity.GetStream(isTcp), e.RemoteEndPoint);
            session.Disconnect();
        }

        DNSEntity GetDNS(String key, DNSEntity entity)
        {
            // 请求父级代理
            IPEndPoint ep = null;
            Boolean isTcp = false;
            Byte[] data = null;
            foreach (var item in Parents)
            {
                isTcp = item.Key == ProtocolType.Tcp;
                var client = NetService.Resolve<ISocketClient>(item.Key);
                // 如果是PTR请求
                if (entity is DNS_PTR)
                {
                    var ptr = entity as DNS_PTR;
                    ptr.Address = item.Value.Address;
                }

                try
                {
                    client.Connect(item.Value);
                    client.Send(entity.GetStream(isTcp), item.Value);
                    data = client.Receive();
                    ep = item.Value;

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
                WriteLog("{0} 返回 {1}", ep, entity2);
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