using System;
using System.Runtime.CompilerServices;
using NewLife.Net.MQTT.Packets;

namespace NewLife.Net.MQTT
{
    /// <summary>签名</summary>
    static class Signatures
    {
        const Byte QoS1Signature = (Int32)QualityOfService.AtLeastOnce << 1;

        // most often used (anticipated) come first

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean IsPublish(Int32 signature)
        {
            const Byte TypeOnlyMask = 0xf << 4;
            return (signature & TypeOnlyMask) == ((Int32)PacketType.PUBLISH << 4);
        }

        public const Byte PubAck = (Int32)PacketType.PUBACK << 4;
        public const Byte PubRec = (Int32)PacketType.PUBREC << 4;
        public const Byte PubRel = ((Int32)PacketType.PUBREL << 4) | QoS1Signature;
        public const Byte PubComp = (Int32)PacketType.PUBCOMP << 4;
        public const Byte Connect = (Int32)PacketType.CONNECT << 4;
        public const Byte ConnAck = (Int32)PacketType.CONNACK << 4;
        public const Byte Subscribe = ((Int32)PacketType.SUBSCRIBE << 4) | QoS1Signature;
        public const Byte SubAck = (Int32)PacketType.SUBACK << 4;
        public const Byte PingReq = (Int32)PacketType.PINGREQ << 4;
        public const Byte PingResp = (Int32)PacketType.PINGRESP << 4;
        public const Byte Disconnect = (Int32)PacketType.DISCONNECT << 4;
        public const Byte Unsubscribe = ((Int32)PacketType.UNSUBSCRIBE << 4) | QoS1Signature;
        public const Byte UnsubAck = (Int32)PacketType.UNSUBACK << 4;
    }
}