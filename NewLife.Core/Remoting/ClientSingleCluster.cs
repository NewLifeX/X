using System;
using System.Threading;
using NewLife.Collections;
using NewLife.Net;

namespace NewLife.Remoting
{
    /// <summary>客户端单连接故障转移集群</summary>
    public class ClientSingleCluster : ICluster<ISocketClient>
    {
        /// <summary>服务器地址列表</summary>
        public String[] Servers { get; set; }

        /// <summary>创建回调</summary>
        public Func<String, ISocketClient> OnCreate { get; set; }

        private ISocketClient _Client;
        /// <summary>从集群中获取资源</summary>
        /// <param name="create"></param>
        /// <returns></returns>
        public ISocketClient Get(Boolean create)
        {
            var tc = _Client;
            if (!create) return tc;

            if (tc != null && tc.Active && !tc.Disposed) return tc;
            lock (this)
            {
                tc = _Client;
                if (tc != null && tc.Active && !tc.Disposed) return tc;

                return _Client = CreateClient();
            }
        }

        /// <summary>归还</summary>
        /// <param name="value"></param>
        public Boolean Put(ISocketClient value) => true;

        /// <summary>Round-Robin 负载均衡</summary>
        private Int32 _index = -1;

        /// <summary>为连接池创建连接</summary>
        /// <returns></returns>
        protected virtual ISocketClient CreateClient()
        {
            // 遍历所有服务，找到可用服务端
            var svrs = Servers;
            if (svrs == null || svrs.Length == 0) throw new InvalidOperationException("没有设置服务端地址Servers");

            var idx = Interlocked.Increment(ref _index);
            Exception last = null;
            for (var i = 0; i < svrs.Length; i++)
            {
                // Round-Robin 负载均衡
                var k = (idx + i) % svrs.Length;
                var svr = svrs[k];
                try
                {
                    var client = OnCreate(svr);
                    client.Open();

                    return client;
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            throw last;
        }
    }
}