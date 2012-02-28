using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.TDP
{
    [Flags]
    public enum TDPFlags : byte
    {
        /// <summary>发端完成发送任务。</summary>
        FIN = 1,

        /// <summary>同步序号用来发起一个连接。</summary>
        SYN = 2,

        /// <summary>重建连接。</summary>
        RST = 4,

        /// <summary>接收方应该尽快将这个报文段交给应用层。</summary>
        PSH = 8,

        /// <summary>确认序号有效。</summary>
        ACK = 16,

        /// <summary>连接保活标志，用于表示TDP 连接通路存活状态。</summary>
        LIV = 32
    }
}