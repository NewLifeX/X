using System;
using System.Net;
using NewLife.Queue.Broker;

namespace NewLife.Queue.Utilities
{
    public class MessageIdUtil
    {
        private static byte[] _ipBytes;
        private static byte[] _portBytes;

        public static string CreateMessageId(long messagePosition)
        {
            if (_ipBytes == null)
            {
                _ipBytes = BrokerService.Instance.Setting.BrokerInfo.ProducerAddress.ToEndPoint().Address.GetAddressBytes();
            }
            if (_portBytes == null)
            {
                _portBytes = BitConverter.GetBytes(BrokerService.Instance.Setting.BrokerInfo.ProducerAddress.ToEndPoint().Port);
            }
            var positionBytes = BitConverter.GetBytes(messagePosition);
            var messageIdBytes = ByteUtil.Combine(_ipBytes, _portBytes, positionBytes);

            return ObjectId.ToHexString(messageIdBytes);
        }
        public static MessageIdInfo ParseMessageId(string messageId)
        {
            var messageIdBytes = ObjectId.ParseHexString(messageId);
            var ipBytes = new byte[4];
            var portBytes = new byte[4];
            var messagePositionBytes = new byte[8];

            Buffer.BlockCopy(messageIdBytes, 0, ipBytes, 0, 4);
            Buffer.BlockCopy(messageIdBytes, 4, portBytes, 0, 4);
            Buffer.BlockCopy(messageIdBytes, 8, messagePositionBytes, 0, 8);

            var ip = BitConverter.ToInt32(ipBytes, 0);
            var port = BitConverter.ToInt32(portBytes, 0);
            var messagePosition = BitConverter.ToInt64(messagePositionBytes, 0);

            return new MessageIdInfo
            {
                IP = new IPAddress(ip),
                Port = port,
                MessagePosition = messagePosition
            };
        }
    }
    public struct MessageIdInfo
    {
        public IPAddress IP;
        public int Port;
        public long MessagePosition;
    }
}
