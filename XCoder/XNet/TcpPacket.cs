using System;
using System.Net.Sockets;
using NewLife.Data;

namespace XCoder.XNet
{
    /// <summary>Tcp包</summary>
    public class TcpPacket
    {
        public IPPacket IPPacket;

        public UInt16 SrcPort;
        public UInt16 DestPort;
        public UInt32 SequenceNo;
        public UInt32 NextSeqNo;
        public Byte HeadLength;
        public Byte Flag;
        public UInt16 WindowSize;
        public UInt16 CheckSum;
        public UInt16 UrgPtr;
        public Byte[] Option;
        public Packet Data;

        public TcpPacket(IPPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packet.Protocol != ProtocolType.Tcp) throw new NotSupportedException();

            IPPacket = packet;

            var pk = packet.Data;
            var data = pk.ReadBytes(0, 20);

            SrcPort = (UInt16)((data[0] << 8) + data[1]);
            DestPort = (UInt16)((data[2] << 8) + data[3]);

            SequenceNo = ((UInt32)data[7] << 24) + ((UInt32)data[6] << 16) + ((UInt32)data[5] << 8) + data[4];
            NextSeqNo = ((UInt32)data[11] << 24) + ((UInt32)data[10] << 16) + ((UInt32)data[9] << 8) + data[8];

            HeadLength = (Byte)(((data[12] & 0xF0) >> 4) * 4);

            // 6bit保留位
            Flag = (Byte)(data[13] & 0x3F);
            WindowSize = (UInt16)((data[14] << 8) + data[15]);
            CheckSum = (UInt16)((data[16] << 8) + data[17]);
            UrgPtr = (UInt16)((data[18] << 8) + data[19]);

            // 可选项
            if (HeadLength > 20) Option = pk.ReadBytes(20, HeadLength - 20);

            Data = pk.Sub(HeadLength);
        }

        public override String ToString() => $"{IPPacket?.SrcAddr}:{SrcPort} => {IPPacket.DestAddr}:{DestPort} [{IPPacket.DataLength - HeadLength}]";
    }
}