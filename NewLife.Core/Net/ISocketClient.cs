using System;

namespace NewLife.Net
{
    /// <summary>Socket客户端</summary>
    /// <remarks>
    /// 具备打开关闭
    /// </remarks>
    public interface ISocketClient : ISocketRemote
    {
        #region 属性
        /// <summary>超时。默认3000ms</summary>
        Int32 Timeout { get; set; }

        /// <summary>是否活动</summary>
        Boolean Active { get; set; }
        #endregion

        #region 开关连接
        /// <summary>打开连接</summary>
        /// <returns>是否成功</returns>
        Boolean Open();

        /// <summary>关闭连接</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns>是否成功</returns>
        Boolean Close(String reason);

        /// <summary>打开后触发。</summary>
        event EventHandler Opened;

        /// <summary>关闭后触发。可实现掉线重连</summary>
        event EventHandler Closed;
        #endregion
    }
}