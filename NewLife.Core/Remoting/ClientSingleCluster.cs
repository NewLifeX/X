﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NewLife.Collections;
using NewLife.Net;

namespace NewLife.Remoting
{
    /// <summary>客户端单连接故障转移集群</summary>
    public class ClientSingleCluster : ICluster<String, ISocketClient>
    {
        /// <summary>服务器地址列表</summary>
        public Func<IEnumerable<String>> GetItems { get; set; }

        /// <summary>创建回调</summary>
        public Func<String, ISocketClient> OnCreate { get; set; }

        /// <summary>打开</summary>
        public virtual Boolean Open() => true;

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns>是否成功</returns>
        public virtual Boolean Close(String reason) => _Client == null ? false : _Client.Close(reason);

        private ISocketClient _Client;
        /// <summary>从集群中获取资源</summary>
        /// <returns></returns>
        public virtual ISocketClient Get()
        {
            var tc = _Client;
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
        public virtual Boolean Put(ISocketClient value) => true;

        /// <summary>Round-Robin 负载均衡</summary>
        private Int32 _index = -1;

        /// <summary>为连接池创建连接</summary>
        /// <returns></returns>
        protected virtual ISocketClient CreateClient()
        {
            // 遍历所有服务，找到可用服务端
            var svrs = GetItems().ToArray();
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