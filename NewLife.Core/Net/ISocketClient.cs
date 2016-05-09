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
        /// <summary>超时时间</summary>
        Int32 Timeout { get; set; }

        /// <summary>是否活动</summary>
        Boolean Active { get; set; }
        #endregion

        #region 开关连接
        /// <summary>打开</summary>
        /// <returns>是否成功</returns>
        Boolean Open();

        /// <summary>关闭</summary>
        /// <returns>是否成功</returns>
        Boolean Close(String reason = null);

        /// <summary>打开后触发。</summary>
        event EventHandler Opened;

        /// <summary>关闭后触发。可实现掉线重连</summary>
        event EventHandler Closed;
        #endregion

        #region 异步接收
        /// <summary>是否异步接收数据</summary>
        Boolean UseReceiveAsync { get; }
        #endregion
    }
}