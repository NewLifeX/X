using System.Collections.Generic;

namespace NewLife.Net.Proxy
{
    /// <summary>数据转发代理接口</summary>
    public interface IProxy
    {
        #region 属性
        /// <summary>会话集合。</summary>
        ICollection<IProxySession> Sessions { get; }

        /// <summary>代理过滤器集合。</summary>
        ICollection<IProxyFilter> Filters { get; }

        /// <summary>主过滤器，同时也是集合，会话主要针对这个操作</summary>
        IProxyFilter MainFilter { get; }
        #endregion

        #region 方法
        #endregion
    }
}