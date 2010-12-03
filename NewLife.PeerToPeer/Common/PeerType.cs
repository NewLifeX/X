using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Common
{
    /// <summary>
    /// 对等方类型
    /// </summary>
    public enum PeerType
    {
        /// <summary>
        /// 自己
        /// </summary>
        MySelf,

        /// <summary>
        /// 互联网
        /// </summary>
        Internet,

        /// <summary>
        /// 同一局域网
        /// </summary>
        NearMe,

        /// <summary>
        /// 内网
        /// </summary>
        Intranet,
    }
}
