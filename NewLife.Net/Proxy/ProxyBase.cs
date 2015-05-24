using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>网络数据转发代理基类</summary>
    /// <remarks>
    /// 网络代理分为本地服务器、客户端、远程服务器三种角色，本地服务器负责监听并转发客户端和远程服务器之间的所有数据。
    /// </remarks>
    /// <typeparam name="TProxySession">代理会话类型</typeparam>
    public abstract class ProxyBase<TProxySession> : NetServer<TProxySession>, IProxy
        where TProxySession : ProxySession, new()
    {
        #region 构造函数--老树添加
        /// <summary></summary>
        public ProxyBase()
        {
            //必须要使UseSession = true，否则创建的session对象无Host属性，在ShowSession时，无法获取Host.Name
            UseSession = true;
        }
        #endregion

        #region 业务
        /// <summary>创建会话</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(ISocketSession session)
        {
            var rs = new TProxySession();
            var ps = rs as IProxySession;
            if (ps != null) ps.Proxy = this;

            return rs;
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

        #region 事件
        #endregion
    }
}