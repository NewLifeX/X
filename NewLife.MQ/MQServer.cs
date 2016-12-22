using System;
using System.Collections.Generic;
using NewLife.Model;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>消息队列服务器</summary>
    public class MQServer : DisposeBase, IServer
    {
        #region 属性
        /// <summary>接口服务器</summary>
        public ApiServer Server { get; private set; }

        /// <summary>主体集合</summary>
        public IDictionary<String, Topic> Topics { get; } = new Dictionary<String, Topic>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public MQServer(Int32 port = 2234)
        {
            Server = new ApiServer(2234);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 主要方法
        /// <summary>开始服务</summary>
        public void Start()
        {
            if (Server.Active) return;

            // 编码器
            if (Server.Encoder == null) Server.Encoder = new JsonEncoder();

            // 注册控制器
            Server.Register<UserController>();
            Server.Register<TopicController>();
            Server.Register<MessageController>();

            // 建立引用
            Server["Topics"] = Topics;

            Server.Start();
        }

        /// <summary>停止服务</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public void Stop(String reason)
        {
            Server.Stop(reason ?? (GetType().Name + "Stop"));
        }
        #endregion
    }
}