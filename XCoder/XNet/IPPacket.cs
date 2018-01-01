using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;

namespace XCoder.XNet
{
    /// <summary>IP包</summary>
    public class IPPacket
    {
        public Byte Version;
        public Byte Length;
        public Byte DiffServices;
        public UInt16 DataLength;
        public UInt16 Identification;
        public Byte Flag;
        public UInt16 Excursion;
        public Byte TTL;
        public ProtocolType Protocol;
        public UInt16 CheckSum;
        public IPAddress SrcAddr;
        public IPAddress DestAddr;
        public Byte[] Option;
        public Packet Data;

        public IPPacket(Packet pk)
        {
            if (pk == null) throw new ArgumentNullException(nameof(pk));

            var data = pk.ReadBytes(0, 20);

            Version = (Byte)((data[0] & 0xF0) >> 4);
            Length = (Byte)((data[0] & 0x0F) * 4);
            DiffServices = data[1];
            DataLength = (UInt16)((data[2] << 8) + data[3]);
            Identification = (UInt16)((data[4] << 8) + data[5]);
            Flag = (Byte)(data[6] >> 5);
            Excursion = (UInt16)(((data[6] & 0x1F) << 8) + data[7]);
            TTL = data[8];
            Protocol = (ProtocolType)data[9];
            CheckSum = (UInt16)((data[10] << 8) + data[11]);

            SrcAddr = new IPAddress(pk.ReadBytes(12, 4));
            DestAddr = new IPAddress(pk.ReadBytes(16, 4));

            // 可选项
            if (Length > 20) Option = pk.ReadBytes(20, Length - 20);

            Data = pk.Sub(Length, DataLength);
        }

        public override String ToString() => $"{SrcAddr} => {DestAddr} [{DataLength}]";
    }
}