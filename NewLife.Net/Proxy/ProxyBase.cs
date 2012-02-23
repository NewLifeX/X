using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>网络数据转发代理基类</summary>
    /// <typeparam name="TProxySession">代理会话类型</typeparam>
    public abstract class ProxyBase<TProxySession> : NetServer<TProxySession>, IProxy
        where TProxySession : ProxySession, new()
    {
        #region 业务
        /// <summary>创建会话</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(NetEventArgs e)
        {
            var session = new TProxySession();
            var ps = session as IProxySession;
            if (ps != null) ps.Proxy = this;

            return session;
        }

        /// <summary>添加会话。子类可以在添加会话前对会话进行一些处理</summary>
        /// <param name="session"></param>
        protected override void AddSession(INetSession session)
        {
            //(session as IProxySession).Proxy = this;
            var ps = session as IProxySession;
            if (ps != null && ps.Proxy == null) ps.Proxy = this;

            base.AddSession(session);
        }
        #endregion
    }
}