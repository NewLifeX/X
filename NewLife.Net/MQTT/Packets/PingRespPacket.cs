
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>心跳响应</summary>
    public sealed class PingRespPacket : DataPacket
    {
        /// <summary>实例</summary>
        public static readonly PingRespPacket Instance = new PingRespPacket();

        PingRespPacket() { }

        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.PINGRESP;
    }
}