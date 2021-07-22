using System;
using System.Net;

namespace NewLife.Data
{
    /// <summary>数据帧接口</summary>
    public interface IData
    {
        #region 属性
        /// <summary>原始数据包</summary>
        Packet Packet { get; set; }

        /// <summary>远程地址</summary>
        IPEndPoint Remote { get; set; }

        /// <summary>解码后的消息</summary>
        Object Message { get; set; }

        /// <summary>用户自定义数据</summary>
        Object UserState { get; set; }
        #endregion
    }
}