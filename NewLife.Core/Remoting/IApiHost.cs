using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>Api主机</summary>
    public interface IApiHost
    {
        /// <summary>编码器</summary>
        IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        IApiHandler Handler { get; set; }

        /// <summary>过滤器</summary>
        IList<IFilter> Filters { get; }

        /// <summary>接口动作管理器</summary>
        IApiManager Manager { get; }

        /// <summary>执行过滤器</summary>
        /// <param name="msg"></param>
        /// <param name="issend"></param>
        void ExecuteFilter(IMessage msg, Boolean issend);
 }
}