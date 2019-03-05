using System;
using System.Threading;
using NewLife.Collections;
using NewLife.Net;

namespace NewLife.Remoting
{
    /// <summary>客户端连接池负载均衡集群</summary>
    public class ClientPoolCluster : ICluster<ISocketClient>
    {
        /// <summary>服务器地址列表</summary>
        public String[] Servers { get; set; }

        /// <summary>创建回调</summary>
        public Func<String, ISocketClient> OnCreate { get; set; }

        /// <summary>连接池</summary>
        public IPool<ISocketClient> Pool { get; private set; }

        /// <summary>实例化连接池集群</summary>
        public ClientPoolCluster()
        {
            Pool = new MyPool { Host = this };
        }

        /// <summary>从集群中获取资源</summary>
        /// <param name="create"></param>
        /// <returns></returns>
        public ISocketClient Get(Boolean create)
        {
            if (!create)
            {
                var p = Pool as MyPool;
                if (p.FreeCount == 0) return null;
            }

            return Pool.Get();
        }

        /// <summary>归还</summary>
        /// <param name="value"></param>
        public virtual Boolean Put(ISocketClient value)
        {
            if (value == null) return false;

            return Pool.Put(value);
        }

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

        class MyPool : ObjectPool<ISocketClient>
        {
            public ClientPoolCluster Host { get; set; }

            public MyPool()
            {
                // 最小值为0，连接池不再使用栈，只使用队列
                Min = 0;
                Max = 100000;
            }

            protected override ISocketClient OnCreate() => Host.CreateClient();
        }
    }
}