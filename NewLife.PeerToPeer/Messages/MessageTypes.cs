using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public enum MessageTypes
    {
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

        /// <summary>
        /// 邀请
        /// </summary>
        Invite,

        /// <summary>
        /// 邀请响应
        /// </summary>
        InviteResponse,

        /// <summary>
        /// 活跃测试
        /// </summary>
        Ping,

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
        /// 传输文件
        /// </summary>
        TranFile,

        /// <summary>
        /// 传输文件响应
        /// </summary>
        TranFileResponse,

        /// <summary>
        /// 文字信息
        /// </summary>
        Text,

        /// <summary>
        /// 文字信息响应
        /// </summary>
        TextResponse
    }
}