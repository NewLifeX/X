
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>断开连接</summary>
    public sealed class DisconnectPacket : DataPacket
    {
        /// <summary>实例</summary>
        public static readonly DisconnectPacket Instance = new DisconnectPacket();

        DisconnectPacket() { }

        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.DISCONNECT;
    }
}