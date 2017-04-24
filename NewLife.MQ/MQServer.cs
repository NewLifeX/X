using System;
using System.Collections.Generic;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>消息队列服务器</summary>
    public class MQServer : DisposeBase, IServer, IServiceProvider
    {
        #region 属性
        /// <summary>接口服务器</summary>
        public ApiServer Server { get; private set; }

        /// <summary>消息队列主机</summary>
        public MQHost Host { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public MQServer(Int32 port = 2234)
        {
            if (port > 0)
            {
                Server = new ApiServer(port);
                Server.Register<MQSession>();
            }
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 开始停止
        /// <summary>开始服务</summary>
        public void Start()
        {
            if (Server.Active) return;

            if (Server.Provider == null) Server.Provider = this;

            // 注册控制器
            //Server.Register<UserController>();
            //Server.Register<TopicController>();
            //Server.Register<MessageController>();

            Server.Start();
        }

        /// <summary>停止服务</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public void Stop(String reason)
        {
            Server.Stop(reason ?? (GetType().Name + "Stop"));
        }
        #endregion

        #region 业务方法

        #endregion

        #region 服务提供者
        /// <summary>服务提供者</summary>
        public IServiceProvider Provider { get; set; }

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual Object GetService(Type serviceType)
        {
            // 服务类是否当前类的基类
            if (GetType().As(serviceType)) return this;

            return Provider?.GetService(serviceType);
        }
        #endregion
    }
}