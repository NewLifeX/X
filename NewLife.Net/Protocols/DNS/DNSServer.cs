using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Collections;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>DNS服务器</summary>
    public class DNSServer : NetServer
    {
        #region 属性
        private List<IPEndPoint> _Parents;
        /// <summary>上级DNS地址</summary>
        public List<IPEndPoint> Parents { get { return _Parents ?? (_Parents = new List<IPEndPoint>()); } set { _Parents = value; } }
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

            // 解析
            var entity = DNSEntity.Read(e.GetStream());

            // 处理，修改
            WriteLog("{0} 请求 {1}", e.RemoteEndPoint, entity);

            // 读取缓存
            entity = cache.GetItem<DNSEntity>(entity.ToString(), entity, GetDNS);

            // 返回给客户端
            session.Send(entity.GetStream(), e.RemoteEndPoint);
            session.Disconnect();
        }

        DNSEntity GetDNS(String key, DNSEntity entity)
        {
            // 请求父级代理
            IPEndPoint ep = null;
            Byte[] data = null;
            foreach (var item in Parents)
            {
                var client = new UdpClientX();
                // 如果是PTR请求
                if (entity is DNS_PTR)
                {
                    var ptr = entity as DNS_PTR;
                    ptr.Address = item.Address;
                }

                try
                {
                    client.Send(entity.GetStream(), item);
                    data = client.Receive();
                    ep = item;

                    if (data != null && data.Length > 0) break;
                }
                catch { }
            }

            DNSEntity entity2 = null;
            try
            {
                // 解析父级代理返回的数据
                entity2 = DNSEntity.Read(data);

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