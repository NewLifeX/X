using System;
using NewLife.Linq;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>网络数据转发代理基类</summary>
    public abstract class ProxyBase : NetServer
    {
        #region 属性
        private ICollection<IProxySession> _Sessions;
        /// <summary>会话集合。</summary>
        public ICollection<IProxySession> Sessions { get { return _Sessions ?? (_Sessions = new List<IProxySession>()); } }

        private ICollection<IProxyFilter> _Filters;
        /// <summary>过滤器集合。</summary>
        public ICollection<IProxyFilter> Filters { get { return _Filters ?? (_Filters = new List<IProxyFilter>()); } }
        #endregion

        #region 构造
        /// <summary>子类重载实现资源释放逻辑</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            lock (Sessions)
            {
                Sessions.ForEach(e => e.Dispose());
            }

            lock (Filters)
            {
                Filters.ForEach(e => e.Dispose());
            }
        }
        #endregion
    }
}