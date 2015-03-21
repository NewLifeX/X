using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.MQTT
{
    /// <summary>服务质量</summary>
    public enum QualityOfService : byte
    {
        /// <summary>至多一次 	发完即丢弃</summary>
        Q0 = 0,

        /// <summary>至少一次 	需要确认回复</summary>
        Q1,

        /// <summary>只有一次 	需要确认回复</summary>
        Q2,

        /// <summary>保留</summary>
        Q3
    }
}