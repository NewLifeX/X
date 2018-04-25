using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using NewLife.Data;
using NewLife.Exceptions;
using NewLife.Net.Handlers;
using NewLife.Net.MQTT.Packets;

namespace NewLife.Net.MQTT
{
    public sealed class MqttDecoder : ReplayingDecoder<MqttDecoder.ParseState>
    {
        public enum ParseState
        {
            Ready,
            Failed
        }

        readonly Boolean isServer;
        readonly Int32 maxMessageSize;

        public MqttDecoder(Boolean isServer, Int32 maxMessageSize)
            : base(ParseState.Ready)
        {
            this.isServer = isServer;
            this.maxMessageSize = maxMessageSize;
        }

        protected override void Decode(IHandlerContext context, Packet input, List<Object> output)
        {
            try
            {
                switch (this.State)
                {
                    case ParseState.Ready:
                        if (!TryDecodePacket(input, context, out var packet))
                        {
                            this.RequestReplay();
                            return;
                        }

                        output.Add(packet);
                        this.Checkpoint();
                        break;
                    case ParseState.Failed:
                        // read out data until connection is closed
                        input.SkipBytes(input.ReadableBytes);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (DecoderException)
            {
                input.SkipBytes(input.ReadableBytes);
                this.Checkpoint(ParseState.Failed);
                throw;
            }
        }

        Boolean TryDecodePacket(Packet buffer, IHandlerContext context, out DataPacket packet)
        {
            if (!buffer.IsReadable(2)) // packet consists of at least 2 bytes
            {
                packet = null;
                return false;
            }

            Int32 signature = buffer.ReadByte();

            if (!TryDecodeRemainingLength(buffer, out var remainingLength) || !buffer.IsReadable(remainingLength))
            {
                packet = null;
                return false;
            }

            packet = DecodePacketInternal(buffer, signature, ref remainingLength, context);

            if (remainingLength > 0)
            {
                throw new DecoderException($"Declared remaining length is bigger than packet data size by {remainingLength}.");
            }

            return true;
        }

        DataPacket DecodePacketInternal(Packet buffer, Int32 packetSignature, ref Int32 remainingLength, IHandlerContext context)
        {
            if (Signatures.IsPublish(packetSignature))
            {
                var qualityOfService = (QualityOfService)((packetSignature >> 1) & 0x3); // take bits #1 and #2 ONLY and convert them into QoS value
                if (qualityOfService == QualityOfService.Reserved)
                {
                    throw new DecoderException($"Unexpected QoS value of {(Int32)qualityOfService} for {PacketType.PUBLISH} packet.");
                }

                var duplicate = (packetSignature & 0x8) == 0x8; // test bit#3
                var retain = (packetSignature & 0x1) != 0; // test bit#0
                var packet = new PublishPacket(qualityOfService, duplicate, retain);
                DecodePublishPacket(buffer, packet, ref remainingLength);
                return packet;
            }

            switch (packetSignature) // strict match checks for valid message type + correct values in flags part
            {
                case Signatures.PubAck:
                    var pubAckPacket = new PubAckPacket();
                    DecodePacketIdVariableHeader(buffer, pubAckPacket, ref remainingLength);
                    return pubAckPacket;
                case Signatures.PubRec:
                    var pubRecPacket = new PubRecPacket();
                    DecodePacketIdVariableHeader(buffer, pubRecPacket, ref remainingLength);
                    return pubRecPacket;
                case Signatures.PubRel:
                    var pubRelPacket = new PubRelPacket();
                    DecodePacketIdVariableHeader(buffer, pubRelPacket, ref remainingLength);
                    return pubRelPacket;
                case Signatures.PubComp:
                    var pubCompPacket = new PubCompPacket();
                    DecodePacketIdVariableHeader(buffer, pubCompPacket, ref remainingLength);
                    return pubCompPacket;
                case Signatures.PingReq:
                    ValidateServerPacketExpected(packetSignature);
                    return PingReqPacket.Instance;
                case Signatures.Subscribe:
                    ValidateServerPacketExpected(packetSignature);
                    var subscribePacket = new SubscribePacket();
                    DecodePacketIdVariableHeader(buffer, subscribePacket, ref remainingLength);
                    DecodeSubscribePayload(buffer, subscribePacket, ref remainingLength);
                    return subscribePacket;
                case Signatures.Unsubscribe:
                    ValidateServerPacketExpected(packetSignature);
                    var unsubscribePacket = new UnsubscribePacket();
                    DecodePacketIdVariableHeader(buffer, unsubscribePacket, ref remainingLength);
                    DecodeUnsubscribePayload(buffer, unsubscribePacket, ref remainingLength);
                    return unsubscribePacket;
                case Signatures.Connect:
                    ValidateServerPacketExpected(packetSignature);
                    var connectPacket = new ConnectPacket();
                    DecodeConnectPacket(buffer, connectPacket, ref remainingLength, context);
                    return connectPacket;
                case Signatures.Disconnect:
                    ValidateServerPacketExpected(packetSignature);
                    return DisconnectPacket.Instance;
                case Signatures.ConnAck:
                    ValidateClientPacketExpected(packetSignature);
                    var connAckPacket = new ConnAckPacket();
                    DecodeConnAckPacket(buffer, connAckPacket, ref remainingLength);
                    return connAckPacket;
                case Signatures.SubAck:
                    ValidateClientPacketExpected(packetSignature);
                    var subAckPacket = new SubAckPacket();
                    DecodePacketIdVariableHeader(buffer, subAckPacket, ref remainingLength);
                    DecodeSubAckPayload(buffer, subAckPacket, ref remainingLength);
                    return subAckPacket;
                case Signatures.UnsubAck:
                    ValidateClientPacketExpected(packetSignature);
                    var unsubAckPacket = new UnsubAckPacket();
                    DecodePacketIdVariableHeader(buffer, unsubAckPacket, ref remainingLength);
                    return unsubAckPacket;
                case Signatures.PingResp:
                    ValidateClientPacketExpected(packetSignature);
                    return PingRespPacket.Instance;
                default:
                    throw new DecoderException($"First packet byte value of `{packetSignature}` is invalid.");
            }
        }

        void ValidateServerPacketExpected(Int32 signature)
        {
            if (!isServer)
            {
                throw new DecoderException($"DataPacket type determined through first packet byte `{signature}` is not supported by MQTT client.");
            }
        }

        void ValidateClientPacketExpected(Int32 signature)
        {
            if (isServer)
            {
                throw new DecoderException($"DataPacket type determined through first packet byte `{signature}` is not supported by MQTT server.");
            }
        }

        Boolean TryDecodeRemainingLength(Packet buffer, out Int32 value)
        {
            Int32 readable = buffer.ReadableBytes;

            var result = 0;
            var multiplier = 1;
            Byte digit;
            var read = 0;
            do
            {
                if (readable < read + 1)
                {
                    value = default(Int32);
                    return false;
                }
                digit = buffer.ReadByte();
                result += (digit & 0x7f) * multiplier;
                multiplier <<= 7;
                read++;
            }
            while ((digit & 0x80) != 0 && read < 4);

            if (read == 4 && (digit & 0x80) != 0)
            {
                throw new DecoderException("Remaining length exceeds 4 bytes in length");
            }

            var completeMessageSize = result + 1 + read;
            if (completeMessageSize > maxMessageSize)
            {
                throw new DecoderException("Message is too big: " + completeMessageSize);
            }

            value = result;
            return true;
        }

        static void DecodeConnectPacket(Packet buffer, ConnectPacket packet, ref Int32 remainingLength, IHandlerContext context)
        {
            var protocolName = DecodeString(buffer, ref remainingLength);
            if (!Util.ProtocolName.Equals(protocolName, StringComparison.Ordinal))
            {
                throw new DecoderException($"Unexpected protocol name. Expected: {Util.ProtocolName}. Actual: {protocolName}");
            }
            packet.ProtocolName = Util.ProtocolName;

            DecreaseRemainingLength(ref remainingLength, 1);
            packet.ProtocolLevel = buffer.ReadByte();

            if (packet.ProtocolLevel != Util.ProtocolLevel)
            {
                var connAckPacket = new ConnAckPacket();
                connAckPacket.ReturnCode = ConnectReturnCode.RefusedUnacceptableProtocolVersion;
                context.WriteAndFlushAsync(connAckPacket);
                throw new DecoderException($"Unexpected protocol level. Expected: {Util.ProtocolLevel}. Actual: {packet.ProtocolLevel}");
            }

            DecreaseRemainingLength(ref remainingLength, 1);
            Int32 connectFlags = buffer.ReadByte();

            packet.CleanSession = (connectFlags & 0x02) == 0x02;

            var hasWill = (connectFlags & 0x04) == 0x04;
            if (hasWill)
            {
                packet.HasWill = true;
                packet.WillRetain = (connectFlags & 0x20) == 0x20;
                packet.WillQualityOfService = (QualityOfService)((connectFlags & 0x18) >> 3);
                if (packet.WillQualityOfService == QualityOfService.Reserved)
                {
                    throw new DecoderException($"[MQTT-3.1.2-14] Unexpected Will QoS value of {(Int32)packet.WillQualityOfService}.");
                }
                packet.WillTopicName = String.Empty;
            }
            else if ((connectFlags & 0x38) != 0) // bits 3,4,5 [MQTT-3.1.2-11]
            {
                throw new DecoderException("[MQTT-3.1.2-11]");
            }

            packet.HasUsername = (connectFlags & 0x80) == 0x80;
            packet.HasPassword = (connectFlags & 0x40) == 0x40;
            if (packet.HasPassword && !packet.HasUsername)
            {
                throw new DecoderException("[MQTT-3.1.2-22]");
            }
            if ((connectFlags & 0x1) != 0) // [MQTT-3.1.2-3]
            {
                throw new DecoderException("[MQTT-3.1.2-3]");
            }

            packet.KeepAliveInSeconds = DecodeUnsignedShort(buffer, ref remainingLength);

            var clientId = DecodeString(buffer, ref remainingLength);
            Util.ValidateClientId(clientId);
            packet.ClientId = clientId;

            if (hasWill)
            {
                packet.WillTopicName = DecodeString(buffer, ref remainingLength);
                var willMessageLength = DecodeUnsignedShort(buffer, ref remainingLength);
                DecreaseRemainingLength(ref remainingLength, willMessageLength);
                packet.WillMessage = buffer.ReadBytes(willMessageLength);
            }

            if (packet.HasUsername)
            {
                packet.Username = DecodeString(buffer, ref remainingLength);
            }

            if (packet.HasPassword)
            {
                packet.Password = DecodeString(buffer, ref remainingLength);
            }
        }

        static void DecodeConnAckPacket(Packet buffer, ConnAckPacket packet, ref Int32 remainingLength)
        {
            var ackData = DecodeUnsignedShort(buffer, ref remainingLength);
            packet.SessionPresent = ((ackData >> 8) & 0x1) != 0;
            packet.ReturnCode = (ConnectReturnCode)(ackData & 0xFF);
        }

        static void DecodePublishPacket(Packet buffer, PublishPacket packet, ref Int32 remainingLength)
        {
            var topicName = DecodeString(buffer, ref remainingLength, 1);
            Util.ValidateTopicName(topicName);

            packet.TopicName = topicName;
            if (packet.QualityOfService > QualityOfService.AtMostOnce)
            {
                DecodePacketIdVariableHeader(buffer, packet, ref remainingLength);
            }

            Packet payload;
            if (remainingLength > 0)
            {
                payload = buffer.ReadSlice(remainingLength);
                payload.Retain();
                remainingLength = 0;
            }
            else
            {
                payload = Unpooled.Empty;
            }
            packet.Payload = payload;
        }

        static void DecodePacketIdVariableHeader(Packet buffer, PacketWithId packet, ref Int32 remainingLength)
        {
            var packetId = packet.PacketId = DecodeUnsignedShort(buffer, ref remainingLength);
            if (packetId == 0)
            {
                throw new DecoderException("[MQTT-2.3.1-1]");
            }
        }

        static void DecodeSubscribePayload(Packet buffer, SubscribePacket packet, ref Int32 remainingLength)
        {
            var subscribeTopics = new List<SubscriptionRequest>();
            while (remainingLength > 0)
            {
                var topicFilter = DecodeString(buffer, ref remainingLength);
                ValidateTopicFilter(topicFilter);

                DecreaseRemainingLength(ref remainingLength, 1);
                Int32 qos = buffer.ReadByte();
                if (qos >= (Int32)QualityOfService.Reserved)
                {
                    throw new DecoderException($"[MQTT-3.8.3-4]. Invalid QoS value: {qos}.");
                }

                subscribeTopics.Add(new SubscriptionRequest(topicFilter, (QualityOfService)qos));
            }

            if (subscribeTopics.Count == 0)
            {
                throw new DecoderException("[MQTT-3.8.3-3]");
            }

            packet.Requests = subscribeTopics;
        }

        static void ValidateTopicFilter(String topicFilter)
        {
            var length = topicFilter.Length;
            if (length == 0)
            {
                throw new DecoderException("[MQTT-4.7.3-1]");
            }

            for (var i = 0; i < length; i++)
            {
                var c = topicFilter[i];
                switch (c)
                {
                    case '+':
                        if ((i > 0 && topicFilter[i - 1] != '/') || (i < length - 1 && topicFilter[i + 1] != '/'))
                        {
                            throw new DecoderException($"[MQTT-4.7.1-3]. Invalid topic filter: {topicFilter}");
                        }
                        break;
                    case '#':
                        if (i < length - 1 || (i > 0 && topicFilter[i - 1] != '/'))
                        {
                            throw new DecoderException($"[MQTT-4.7.1-2]. Invalid topic filter: {topicFilter}");
                        }
                        break;
                }
            }
        }

        static void DecodeSubAckPayload(Packet buffer, SubAckPacket packet, ref Int32 remainingLength)
        {
            var returnCodes = new QualityOfService[remainingLength];
            for (var i = 0; i < remainingLength; i++)
            {
                var returnCode = (QualityOfService)buffer.ReadByte();
                if (returnCode > QualityOfService.ExactlyOnce && returnCode != QualityOfService.Failure)
                {
                    throw new DecoderException($"[MQTT-3.9.3-2]. Invalid return code: {returnCode}");
                }
                returnCodes[i] = returnCode;
            }
            packet.ReturnCodes = returnCodes;

            remainingLength = 0;
        }

        static void DecodeUnsubscribePayload(Packet buffer, UnsubscribePacket packet, ref Int32 remainingLength)
        {
            var unsubscribeTopics = new List<String>();
            while (remainingLength > 0)
            {
                var topicFilter = DecodeString(buffer, ref remainingLength);
                ValidateTopicFilter(topicFilter);
                unsubscribeTopics.Add(topicFilter);
            }

            if (unsubscribeTopics.Count == 0)
            {
                throw new DecoderException("[MQTT-3.10.3-2]");
            }

            packet.TopicFilters = unsubscribeTopics;

            remainingLength = 0;
        }

        static Int32 DecodeUnsignedShort(Packet buffer, ref Int32 remainingLength)
        {
            DecreaseRemainingLength(ref remainingLength, 2);
            return buffer.ReadUnsignedShort();
        }

        static String DecodeString(Packet buffer, ref Int32 remainingLength) => DecodeString(buffer, ref remainingLength, 0, Int32.MaxValue);

        static String DecodeString(Packet buffer, ref Int32 remainingLength, Int32 minBytes) => DecodeString(buffer, ref remainingLength, minBytes, Int32.MaxValue);

        static String DecodeString(Packet buffer, ref Int32 remainingLength, Int32 minBytes, Int32 maxBytes)
        {
            var size = DecodeUnsignedShort(buffer, ref remainingLength);

            if (size < minBytes)
            {
                throw new DecoderException($"String value is shorter than minimum allowed {minBytes}. Advertised length: {size}");
            }
            if (size > maxBytes)
            {
                throw new DecoderException($"String value is longer than maximum allowed {maxBytes}. Advertised length: {size}");
            }

            if (size == 0)
            {
                return String.Empty;
            }

            DecreaseRemainingLength(ref remainingLength, size);

            var value = buffer.ToString(buffer.ReaderIndex, size, Encoding.UTF8);
            // todo: enforce string definition by MQTT spec
            buffer.SetReaderIndex(buffer.ReaderIndex + size);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // we don't care about the method being on exception's stack so it's OK to inline
        static void DecreaseRemainingLength(ref Int32 remainingLength, Int32 minExpectedLength)
        {
            if (remainingLength < minExpectedLength)
            {
                throw new DecoderException($"Current Remaining Length of {remainingLength} is smaller than expected {minExpectedLength}.");
            }
            remainingLength -= minExpectedLength;
        }
    }
}