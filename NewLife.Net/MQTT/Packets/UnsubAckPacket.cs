
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>取消订阅确认</summary>
    public sealed class UnsubAckPacket : PacketWithId
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.UNSUBACK;

        public static UnsubAckPacket InResponseTo(UnsubscribePacket unsubscribePacket)
        {
            return new UnsubAckPacket
            {
                PacketId = unsubscribePacket.PacketId
            };
        }
    }
}