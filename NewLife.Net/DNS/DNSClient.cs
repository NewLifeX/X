using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Net.DNS
{
    /// <summary>DNS客户端</summary>
    public class DNSClient : DisposeBase
    {
        #region 属性
        /// <summary>DNS服务器</summary>
        public NetUri Server { get; set; }

        /// <summary>网络客户端</summary>
        public ISocketClient Client { get; set; }

        /// <summary>总次数</summary>
        public Int32 Total { get; private set; }

        /// <summary>成功数</summary>
        public Int32 Success { get; private set; }

        /// <summary>成功率</summary>
        public Double Percent { get { return Total == 0 ? 0 : (Double)Success / Total; } }
        #endregion

        #region 构造
        /// <summary>使用本地DNS配置创建DNS客户端</summary>
        public DNSClient()
        {
            var addr = NetHelper.GetDns().FirstOrDefault();
            if (addr != null) Server = new NetUri(NetType.Udp, addr, 53);
        }

        /// <summary>使用指定目标服务器创建DNS客户端</summary>
        /// <param name="uri"></param>
        public DNSClient(NetUri uri)
        {
            Server = uri;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Client.TryDispose();
        }
        #endregion

        #region 打开关闭
        /// <summary>打开客户端</summary>
        /// <returns></returns>
        public Boolean Open()
        {
            if (Server == null) return false;

            var nc = Client;
            if (nc == null || nc.Disposed) nc = Client = Server.CreateRemote();
            if (nc == null) return false;
            if (nc.Active) return true;

            nc.Timeout = 3000;

            nc.Received += Client_Received;

            return nc.Open();
        }

        private void Client_Received(Object sender, ReceivedEventArgs e)
        {
            if (e.Length == 0) return;

            var dns = DNSEntity.Read(e.Stream, Client.Local.IsTcp);
            OnReceive(dns);
        }

        /// <summary>关闭客户端</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns></returns>
        public Boolean Close(String reason)
        {
            var nc = Client;
            if (nc == null || !nc.Active) return true;

            nc.Received -= Client_Received;

            return nc.Close(reason);
        }
        #endregion

        #region 主要方法
        private TaskCompletionSource<DNSEntity> _recv;

        /// <summary>收到响应</summary>
        /// <param name="dns"></param>
        protected virtual void OnReceive(DNSEntity dns)
        {
            if (dns != null) Success++;

            _recv?.SetResult(dns);
            _recv = null;
        }

        /// <summary>异步查询解析</summary>
        /// <param name="dns"></param>
        /// <returns></returns>
        public virtual async Task<DNSEntity> QueryAsync(DNSEntity dns)
        {
            if (!Open()) return null;

            var nc = Client;

            var tcs = new TaskCompletionSource<DNSEntity>();
            _recv = tcs;

            // 发送请求
            var ms = dns.GetStream(nc.Local.IsTcp);
            nc.Send(ms.ReadBytes());

            Total++;

            return await tcs.Task;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return "{0} {1:n0}/{2:n0}={3:p0}".F(Server, Success, Total, Percent);
        }
        #endregion

        #region 静态
        /// <summary>使用一批DNS客户端发起请求</summary>
        /// <param name="clients"></param>
        /// <param name="dns"></param>
        /// <param name="msTimeout"></param>
        /// <returns></returns>
        public static IDictionary<DNSClient, DNSEntity> QueryAll(IEnumerable<DNSClient> clients, DNSEntity dns, Int32 msTimeout = 1000)
        {
            DNSClient[] cs = null;
            lock (clients) { cs = clients.ToArray(); }

            // 所有客户端同时发出请求
            var ts = new List<Task<DNSEntity>>();
            foreach (var client in cs)
            {
                var task = client.QueryAsync(dns);
                ts.Add(task);
            }

            // 等待所有客户端响应，或者超时
            Task.WaitAll(ts.ToArray(), msTimeout);

            // 获取结果
            var dic = new Dictionary<DNSClient, DNSEntity>();
            for (var i = 0; i < cs.Length; i++)
            {
                var task = ts[i];
                if (task.IsOK() && task.Result != null) dic.Add(cs[i], task.Result);
            }

            return dic;
        }
        #endregion
    }
}