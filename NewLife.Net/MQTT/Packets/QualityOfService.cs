
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>服务质量</summary>
    public enum QualityOfService
    {
        /// <summary>最多一次</summary>
        AtMostOnce = 0,

        /// <summary>至少一次</summary>
        AtLeastOnce = 0x1,

        /// <summary>刚好一次</summary>
        ExactlyOnce = 0x2,

        /// <summary>保留</summary>
        Reserved = 0x3,

        /// <summary>失败</summary>
        Failure = 0x80
    }
}