using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 消息类型
    /// </summary>
    /// <remarks>
    /// 所有消息分成四段，便于每一段增加消息
    /// </remarks>
    public enum MessageTypes
    {
        #region 公共消息
        /// <summary>
        /// 未知
        /// </summary>
        Unkown,

        /// <summary>
        /// 测试
        /// </summary>
        Test,

        /// <summary>
        /// 测试响应
        /// </summary>
        TestResponse,
        #endregion

        #region 命令消息
        /// <summary>
        /// 活跃测试
        /// </summary>
        Ping = 0x10,

        /// <summary>
        /// 活跃测试响应
        /// </summary>
        PingResponse,

        /// <summary>
        /// 查找种子
        /// </summary>
        FindTorrent,

        /// <summary>
        /// 查找种子响应
        /// </summary>
        FindTorrentResponse,

        /// <summary>
        /// 文字信息
        /// </summary>
        Text,

        /// <summary>
        /// 文字信息响应
        /// </summary>
        TextResponse,

        #endregion

        #region Tracker通讯消息
        /// <summary>
        /// 跟踪
        /// </summary>
        Track = 0x50,

        /// <summary>
        /// 跟踪响应
        /// </summary>
        TrackResponse,
        #endregion

        #region 客户端互相通讯消息
        /// <summary>
        /// 邀请
        /// </summary>
        Invite = 0x90,

        /// <summary>
        /// 邀请响应
        /// </summary>
        InviteResponse,

        /// <summary>
        /// 传输文件
        /// </summary>
        TranFile,

        /// <summary>
        /// 传输文件响应
        /// </summary>
        TranFileResponse,
        #endregion
    }
}