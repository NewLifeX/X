using System;
using NewLife.Exceptions;

namespace NewLife.Net.MQTT
{
    /// <summary>工具类</summary>
    static class Util
    {
        public const String ProtocolName = "MQTT";
        public const Int32 ProtocolLevel = 4;

        static readonly Char[] TopicWildcards = { '#', '+' };

        public static void ValidateTopicName(String topicName)
        {
            if (topicName.Length == 0)
            {
                throw new DecoderException("[MQTT-4.7.3-1]");
            }

            if (topicName.IndexOfAny(TopicWildcards) > 0)
            {
                throw new DecoderException($"Invalid PUBLISH topic name: {topicName}");
            }
        }

        public static void ValidatePacketId(Int32 packetId)
        {
            if (packetId < 1)
            {
                throw new DecoderException("Invalid packet identifier: " + packetId);
            }
        }

        public static void ValidateClientId(String clientId)
        {
            if (clientId == null)
            {
                throw new DecoderException("Client identifier is required.");
            }
        }
    }
}