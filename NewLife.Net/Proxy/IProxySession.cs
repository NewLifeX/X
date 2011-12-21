using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Proxy
{
    /// <summary>代理会话接口。客户端的一次转发请求（或者Tcp连接），就是一个会话。转发的全部操作都在会话中完成。</summary>
    /// <remarks>
    /// 一个会话应该包含两端，两个Socket，服务端和客户端
    /// </remarks>
    public interface IProxySession
    {
        #region 属性
        #endregion
    }
}