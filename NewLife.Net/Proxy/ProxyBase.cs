using System.IO;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>网络数据转发代理基类</summary>
    public abstract class ProxyBase : NetServer<ProxySession>, IProxy
    {
        #region 业务
        ///// <summary>创建会话</summary>
        ///// <param name="e"></param>
        ///// <returns></returns>
        //protected override INetSession CreateSession(NetEventArgs e)
        //{
        //    var session = new ProxySession();
        //    session.Proxy = this;

        //    return session;
        //}

        /// <summary>添加会话。子类可以在添加会话前对会话进行一些处理</summary>
        /// <param name="session"></param>
        protected override void AddSession(INetSession session)
        {
            (session as IProxySession).Proxy = this;
            base.AddSession(session);
        }
        #endregion
    }
}