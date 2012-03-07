using System;
using NewLife.IO;
using NewLife.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using NewLife.Collections;
using NewLife.Net.Common;
using NewLife.Net.Sockets;
using System.Net;

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
                        list.Add(new NetUri(ProtocolType.Udp, item, 53));
                    }
                    list.Add(new NetUri("tcp://8.8.8.8"));
                    list.Add(new NetUri("udp://4.4.4.4"));

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
                ps.Clear();

                if (value.IsNullOrWhiteSpace()) return;

                var ss = value.Split(",");
                if (ss == null || ss.Length < 1) return;

                foreach (var item in ss)
                {
                    var uri = new NetUri(item);
                    ps.Add(uri);
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
            var entity = DNSEntity.Read(e.GetStream(), isTcp);

            // 处理，修改
            WriteLog("{0}://{1} 请求 {2}", session.ProtocolType, e.RemoteEndPoint, entity);

            // 如果是PTR请求
            if (entity.Type == DNSQueryType.PTR)
            {
                var ptr = entity.Questions[0] as DNS_PTR;
                // 对本地的请求马上返回
                var addr = ptr.Address;
                if (IPAddress.IsLoopback(addr) || NetHelper.GetIPs().Any(ip => ip + "" == addr + ""))
                {
                    var ptr2 = new DNS_PTR();
                    ptr2.Name = ptr.Name;
                    ptr2.DomainName = DomainName;

                    var rs = new DNSEntity();
                    rs.Questions = entity.Questions;
                    rs.Answers = new DNSRecord[] { ptr2 };
                    //var aw = rs.GetAnswer(true);
                    //aw.Type = DNSQueryType.PTR;
                    //aw.Name = ptr.Name;

                    rs.Header.ID = entity.Header.ID;
                    session.Send(rs.GetStream(isTcp));
                    return;
                }
            }

            // 读取缓存
            var entity2 = cache.GetItem<DNSEntity>(entity.ToString(), entity, GetDNS, false);
            //var entity2 = DNSEntity.Read(File.ReadAllBytes("dns2.bin"), false);

            // 返回给客户端
            if (entity2 != null)
            {
                //var fs = new FileStream("dns.bin", FileMode.CreateNew);
                //entity2.GetStream().CopyTo(fs);
                //fs.Close();

                // 如果是PTR请求
                if (entity.Type == DNSQueryType.PTR && entity2.Type == DNSQueryType.PTR)
                {
                    var ptr = entity.Questions[0] as DNS_PTR;
                    var ptr2 = entity2.GetAnswer() as DNS_PTR;
                    ptr2.Name = ptr.Name;
                    ptr2.DomainName = DomainName;
                    if (entity2.Answers != null && entity2.Answers.Length > 0)
                    {
                        foreach (var item in entity2.Answers)
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
            NetUri parent = null;
            Byte[] data = null;
            foreach (var item in Parents)
            {
                var session = NetService.CreateSession(item);
                parent = item;
                // 如果是PTR请求
                if (entity.Type == DNSQueryType.PTR)
                {
                    // 复制一份，防止修改外部
                    entity = new DNSEntity().CloneFrom(entity);

                    var ptr = entity.GetAnswer() as DNS_PTR;
                    ptr.Address = parent.Address;
                }

                try
                {
                    session.Send(entity.GetStream(item.ProtocolType == ProtocolType.Tcp));
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
                entity2 = DNSEntity.Read(data, parent.ProtocolType == ProtocolType.Tcp);

                // 处理，修改
                WriteLog("{0} 返回 {1}", parent, entity2);
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