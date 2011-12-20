using System;
using System.Collections.Generic;
using System.Text;

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
        #endregion
    }
}