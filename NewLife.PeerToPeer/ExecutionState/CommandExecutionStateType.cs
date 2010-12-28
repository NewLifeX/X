using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 命令执行状态
    /// </summary>
    public enum CommandExecutionStateType
    {
        /// <summary>
        /// 新指令
        /// </summary>
        新指令 = 0,

        /// <summary>
        /// 已下达
        /// </summary>
        已下达 = 1,

        /// <summary>
        /// 执行中
        /// </summary>
        执行中 = 2,

        /// <summary>
        /// 执行完成
        /// </summary>
        执行完成 = 3,

        /// <summary>
        /// 未知错误
        /// </summary>
        未知错误 = 999,

        /// <summary>
        /// 删除指令
        /// </summary>
        删除指令 = 888
    }

}
