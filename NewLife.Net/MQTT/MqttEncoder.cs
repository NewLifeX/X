using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using NewLife.Net.MQTT.Packets;

namespace NewLife.Net.MQTT
{
    public sealed class MqttEncoder : MessageToMessageEncoder<DataPacket>
    {
        public static readonly MqttEncoder Instance = new MqttEncoder();
        const Int32 PacketIdLength = 2;
        const Int32 StringSizeLength = 2;
        const Int32 MaxVariableLength = 4;

        protected override void Encode(IHandlerContext context, DataPacket message, List<Object> output) => DoEncode(context.Allocator, message, output);

        public override Boolean IsSharable => true;

        /// <summary>
        ///     This is the main encoding method.
        ///     It's only visible for testing.
        ///     @param bufferAllocator Allocates ByteBuf
        ///     @param packet MQTT packet to encode
        ///     @return ByteBuf with encoded bytes
        /// </summary>
        internal static void DoEncode(IByteBufferAllocator bufferAllocator, DataPacket packet, List<Object> output)
        {
            switch (packet.PacketType)
            {
                case PacketType.CONNECT:
                    EncodeConnectMessage(bufferAllocator, (ConnectPacket)packet, output);
                    break;
                case PacketType.CONNACK:
                    EncodeConnAckMessage(bufferAllocator, (ConnAckPacket)packet, output);
                    break;
                case PacketType.PUBLISH:
                    EncodePublishMessage(bufferAllocator, (PublishPacket)packet, output);
                    break;
                case PacketType.PUBACK:
                case PacketType.PUBREC:
                case PacketType.PUBREL:
                case PacketType.PUBCOMP:
                case PacketType.UNSUBACK:
                    EncodePacketWithIdOnly(bufferAllocator, (PacketWithId)packet, output);
                    break;
                case PacketType.SUBSCRIBE:
                    EncodeSubscribeMessage(bufferAllocator, (SubscribePacket)packet, output);
                    break;
                case PacketType.SUBACK:
                    EncodeSubAckMessage(bufferAllocator, (SubAckPacket)packet, output);
                    break;
                case PacketType.UNSUBSCRIBE:
                    EncodeUnsubscribeMessage(bufferAllocator, (UnsubscribePacket)packet, output);
                    break;
                case PacketType.PINGREQ:
                case PacketType.PINGRESP:
                case PacketType.DISCONNECT:
                    EncodePacketWithFixedHeaderOnly(bufferAllocator, packet, output);
                    break;
                default:
                    throw new ArgumentException("Unknown packet type: " + packet.PacketType, nameof(packet));
            }
        }

        static void EncodeConnectMessage(IByteBufferAllocator bufferAllocator, ConnectPacket packet, List<Object> output)
        {
            var payloadBufferSize = 0;

            // Client id
            var clientId = packet.ClientId;
            Util.ValidateClientId(clientId);
            var clientIdBytes = EncodeStringInUtf8(clientId);
            payloadBufferSize += StringSizeLength + clientIdBytes.Length;

            Byte[] willTopicBytes;
            Packet willMessage;
            if (packet.HasWill)
            {
                // Will topic and message
                var willTopic = packet.WillTopicName;
                willTopicBytes = EncodeStringInUtf8(willTopic);
                willMessage = packet.WillMessage;
                payloadBufferSize += StringSizeLength + willTopicBytes.Length;
                payloadBufferSize += 2 + willMessage.ReadableBytes;
            }
            else
            {
                willTopicBytes = null;
                willMessage = null;
            }

            var userName = packet.Username;
            Byte[] userNameBytes;
            if (packet.HasUsername)
            {
                userNameBytes = EncodeStringInUtf8(userName);
                payloadBufferSize += StringSizeLength + userNameBytes.Length;
            }
            else
            {
                userNameBytes = null;
            }

            Byte[] passwordBytes;
            if (packet.HasPassword)
            {
                var password = packet.Password;
                passwordBytes = EncodeStringInUtf8(password);
                payloadBufferSize += StringSizeLength + passwordBytes.Length;
            }
            else
            {
                passwordBytes = null;
            }

            // Fixed header
            var protocolNameBytes = EncodeStringInUtf8(Util.ProtocolName);
            var variableHeaderBufferSize = StringSizeLength + protocolNameBytes.Length + 4;
            var variablePartSize = variableHeaderBufferSize + payloadBufferSize;
            var fixedHeaderBufferSize = 1 + MaxVariableLength;
            Packet buf = null;
            try
            {
                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buf, variablePartSize);

                buf.WriteShort(protocolNameBytes.Length);
                buf.WriteBytes(protocolNameBytes);

                buf.WriteByte(Util.ProtocolLevel);
                buf.WriteByte(CalculateConnectFlagsByte(packet));
                buf.WriteShort(packet.KeepAliveInSeconds);

                // Payload
                buf.WriteShort(clientIdBytes.Length);
                buf.WriteBytes(clientIdBytes, 0, clientIdBytes.Length);
                if (packet.HasWill)
                {
                    buf.WriteShort(willTopicBytes.Length);
                    buf.WriteBytes(willTopicBytes, 0, willTopicBytes.Length);
                    buf.WriteShort(willMessage.ReadableBytes);
                    if (willMessage.IsReadable())
                    {
                        buf.WriteBytes(willMessage);
                    }
                    willMessage.Release();
                    willMessage = null;
                }
                if (packet.HasUsername)
                {
                    buf.WriteShort(userNameBytes.Length);
                    buf.WriteBytes(userNameBytes, 0, userNameBytes.Length);

                    if (packet.HasPassword)
                    {
                        buf.WriteShort(passwordBytes.Length);
                        buf.WriteBytes(passwordBytes, 0, passwordBytes.Length);
                    }
                }

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
                willMessage?.SafeRelease();
            }
        }

        static Int32 CalculateConnectFlagsByte(ConnectPacket packet)
        {
            var flagByte = 0;
            if (packet.HasUsername)
            {
                flagByte |= 0x80;
            }
            if (packet.HasPassword)
            {
                flagByte |= 0x40;
            }
            if (packet.HasWill)
            {
                flagByte |= 0x04;
                flagByte |= ((Int32)packet.WillQualityOfService & 0x03) << 3;
                if (packet.WillRetain)
                {
                    flagByte |= 0x20;
                }
            }
            if (packet.CleanSession)
            {
                flagByte |= 0x02;
            }
            return flagByte;
        }

        static void EncodeConnAckMessage(IByteBufferAllocator bufferAllocator, ConnAckPacket message, List<Object> output)
        {
            Packet buffer = null;
            try
            {
                buffer = bufferAllocator.Buffer(4);
                buffer.WriteByte(CalculateFirstByteOfFixedHeader(message));
                buffer.WriteByte(2); // remaining length
                if (message.SessionPresent)
                {
                    buffer.WriteByte(1); // 7 reserved 0-bits and SP = 1
                }
                else
                {
                    buffer.WriteByte(0); // 7 reserved 0-bits and SP = 0
                }
                buffer.WriteByte((Byte)message.ReturnCode);


                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        static void EncodePublishMessage(IByteBufferAllocator bufferAllocator, PublishPacket packet, List<Object> output)
        {
            Packet payload = packet.Payload ?? Unpooled.Empty;

            var topicName = packet.TopicName;
            Util.ValidateTopicName(topicName);
            var topicNameBytes = EncodeStringInUtf8(topicName);

            Int32 variableHeaderBufferSize = StringSizeLength + topicNameBytes.Length +
                (packet.QualityOfService > QualityOfService.AtMostOnce ? PacketIdLength : 0);
            Int32 payloadBufferSize = payload.ReadableBytes;
            var variablePartSize = variableHeaderBufferSize + payloadBufferSize;
            var fixedHeaderBufferSize = 1 + MaxVariableLength;

            Packet buf = null;
            try
            {
                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buf, variablePartSize);
                buf.WriteShort(topicNameBytes.Length);
                buf.WriteBytes(topicNameBytes);
                if (packet.QualityOfService > QualityOfService.AtMostOnce)
                {
                    buf.WriteShort(packet.PacketId);
                }

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
            }

            if (payload.IsReadable())
            {
                output.Add(payload.Retain());
            }
        }

        static void EncodePacketWithIdOnly(IByteBufferAllocator bufferAllocator, PacketWithId packet, List<Object> output)
        {
            var msgId = packet.PacketId;

            const Int32 VariableHeaderBufferSize = PacketIdLength; // variable part only has a packet id
            var fixedHeaderBufferSize = 1 + MaxVariableLength;
            Packet buffer = null;
            try
            {
                buffer = bufferAllocator.Buffer(fixedHeaderBufferSize + VariableHeaderBufferSize);
                buffer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buffer, VariableHeaderBufferSize);
                buffer.WriteShort(msgId);

                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        static void EncodeSubscribeMessage(IByteBufferAllocator bufferAllocator, SubscribePacket packet, List<Object> output)
        {
            const Int32 VariableHeaderSize = PacketIdLength;
            var payloadBufferSize = 0;

            ThreadLocalObjectList encodedTopicFilters = ThreadLocalObjectList.NewInstance();

            Packet buf = null;
            try
            {
                foreach (var topic in packet.Requests)
                {
                    var topicFilterBytes = EncodeStringInUtf8(topic.TopicFilter);
                    payloadBufferSize += StringSizeLength + topicFilterBytes.Length + 1; // length, value, QoS
                    encodedTopicFilters.Add(topicFilterBytes);
                }

                var variablePartSize = VariableHeaderSize + payloadBufferSize;
                var fixedHeaderBufferSize = 1 + MaxVariableLength;

                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buf, variablePartSize);

                // Variable Header
                buf.WriteShort(packet.PacketId); // todo: review: validate?

                // Payload
                for (var i = 0; i < encodedTopicFilters.Count; i++)
                {
                    var topicFilterBytes = (Byte[])encodedTopicFilters[i];
                    buf.WriteShort(topicFilterBytes.Length);
                    buf.WriteBytes(topicFilterBytes, 0, topicFilterBytes.Length);
                    buf.WriteByte((Int32)packet.Requests[i].QualityOfService);
                }

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
                encodedTopicFilters.Return();
            }
        }

        static void EncodeSubAckMessage(IByteBufferAllocator bufferAllocator, SubAckPacket message, List<Object> output)
        {
            var payloadBufferSize = message.ReturnCodes.Count;
            var variablePartSize = PacketIdLength + payloadBufferSize;
            var fixedHeaderBufferSize = 1 + MaxVariableLength;
            Packet buf = null;
            try
            {
                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte(CalculateFirstByteOfFixedHeader(message));
                WriteVariableLengthInt(buf, variablePartSize);
                buf.WriteShort(message.PacketId);
                foreach (QualityOfService qos in message.ReturnCodes)
                {
                    buf.WriteByte((Byte)qos);
                }

                output.Add(buf);
                buf = null;

            }
            finally
            {
                buf?.SafeRelease();
            }
        }

        static void EncodeUnsubscribeMessage(IByteBufferAllocator bufferAllocator, UnsubscribePacket packet, List<Object> output)
        {
            const Int32 VariableHeaderSize = 2;
            var payloadBufferSize = 0;

            ThreadLocalObjectList encodedTopicFilters = ThreadLocalObjectList.NewInstance();

            Packet buf = null;
            try
            {
                foreach (var topic in packet.TopicFilters)
                {
                    var topicFilterBytes = EncodeStringInUtf8(topic);
                    payloadBufferSize += StringSizeLength + topicFilterBytes.Length; // length, value
                    encodedTopicFilters.Add(topicFilterBytes);
                }

                var variablePartSize = VariableHeaderSize + payloadBufferSize;
                var fixedHeaderBufferSize = 1 + MaxVariableLength;

                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buf, variablePartSize);

                // Variable Header
                buf.WriteShort(packet.PacketId); // todo: review: validate?

                // Payload
                for (var i = 0; i < encodedTopicFilters.Count; i++)
                {
                    var topicFilterBytes = (Byte[])encodedTopicFilters[i];
                    buf.WriteShort(topicFilterBytes.Length);
                    buf.WriteBytes(topicFilterBytes, 0, topicFilterBytes.Length);
                }

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
                encodedTopicFilters.Return();
            }
        }

        static void EncodePacketWithFixedHeaderOnly(IByteBufferAllocator bufferAllocator, DataPacket packet, List<Object> output)
        {
            Packet buffer = null;
            try
            {
                buffer = bufferAllocator.Buffer(2);
                buffer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                buffer.WriteByte(0);

                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        static Int32 CalculateFirstByteOfFixedHeader(DataPacket packet)
        {
            var ret = 0;
            ret |= (Int32)packet.PacketType << 4;
            if (packet.Duplicate)
            {
                ret |= 0x08;
            }
            ret |= (Int32)packet.QualityOfService << 1;
            if (packet.RetainRequested)
            {
                ret |= 0x01;
            }
            return ret;
        }

        static void WriteVariableLengthInt(Packet buffer, Int32 value)
        {
            do
            {
                var digit = value % 128;
                value /= 128;
                if (value > 0)
                {
                    digit |= 0x80;
                }
                buffer.WriteByte(digit);
            }
            while (value > 0);
        }

        static Byte[] EncodeStringInUtf8(String s)
        {
            // todo: validate against extra limitations per MQTT's UTF-8 string definition
            return Encoding.UTF8.GetBytes(s);
        }
    }
}