using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>DNS服务器</summary>
    public class DNSServer : NetAppServer
    {
        #region 属性
        private List<IPEndPoint> _Parents;
        /// <summary>上级DNS地址</summary>
        public List<IPEndPoint> Parents { get { return _Parents ?? (_Parents = new List<IPEndPoint>()); } set { _Parents = value; } }
        #endregion

        #region 构造

        #endregion

        #region 方法
        /// <summary>接收处理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            var session = sender as ISocketSession;

            // 解析
            var entity = DNSEntity.Read(e.GetStream());

            // 处理，修改
            WriteLog("{0} 请求 {1}", e.RemoteEndPoint, entity);

            // 重新封装为二进制
            var ms = new MemoryStream();
            entity.Write(ms);
            Byte[] data = null;

            // 请求父级代理
            foreach (var item in Parents)
            {
                var client = new UdpClientX();
                ms.Position = 0;

                try
                {
                    client.Send(ms, item);
                    data = client.Receive();

                    if (data != null && data.Length > 0) break;
                }
                catch { }
            }

            // 解析父级代理返回的数据
            var entity2 = DNSEntity.Read(new MemoryStream(data));

            // 处理，修改
            WriteLog("上级返回 {0}", entity2);

            // 重新封装为二进制
            ms = new MemoryStream();
            entity2.Write(ms);

            // 返回给客户端
            ms.Position = 0;
            session.Send(ms, e.RemoteEndPoint);
            session.Disconnect();
        }
        #endregion
    }
}