
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>心跳请求</summary>
    public sealed class PingReqPacket : DataPacket
    {
        /// <summary>实例</summary>
        public static readonly PingReqPacket Instance = new PingReqPacket();

        PingReqPacket() { }

        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.PINGREQ;
    }
}