using System;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    /// <remarks>
    /// 在基于令牌Token的无状态验证模式中，可以借助Token重写IApiHandler.Prepare，来达到同一个Token共用相同的IApiSession.Items
    /// </remarks>
    public interface IApiSession : IExtend
    {
        /// <summary>主机</summary>
        IApiHost Host { get; }

        /// <summary>最后活跃时间</summary>
        DateTime LastActive { get; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] AllSessions { get; }

        /// <summary>令牌</summary>
        String Token { get; set; }

        /// <summary>请求参数</summary>
        IDictionary<String, Object> Parameters { get; set; }

        ///// <summary>获取/设置 用户会话数据</summary>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //Object this[String key] { get; set; }

        /// <summary>查找Api动作</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        ApiAction FindAction(String action);

        /// <summary>创建控制器实例</summary>
        /// <param name="api"></param>
        /// <returns></returns>
        Object CreateController(ApiAction api);
    }
}