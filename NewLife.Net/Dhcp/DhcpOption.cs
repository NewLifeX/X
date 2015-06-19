using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.Dhcp
{
    /// <summary>DHCP选项类型</summary>
    public enum DhcpOptions : byte
    {
        /// <summary></summary>
        Mask = 1,

        /// <summary></summary>
        Router = 3,

        /// <summary></summary>
        TimeServer = 4,

        /// <summary></summary>
        NameServer = 5,

        /// <summary></summary>
        DNSServer = 6,

        /// <summary></summary>
        LOGServer = 7,

        /// <summary></summary>
        HostName = 12,

        /// <summary></summary>
        MTU = 26,				// 0x1A

        /// <summary></summary>
        StaticRout = 33,		// 0x21

        /// <summary></summary>
        ARPCacheTimeout = 35,	// 0x23

        /// <summary></summary>
        NTPServer = 42,		// 0x2A

        /// <summary></summary>
        RequestedIP = 50,		// 0x32

        /// <summary></summary>
        IPLeaseTime = 51,		// 0x33

        /// <summary></summary>
        MessageType = 53,		// 0x35

        /// <summary></summary>
        DHCPServer = 54,		// 0x36

        /// <summary></summary>
        ParameterList = 55,	// 0x37

        /// <summary></summary>
        Message = 56,			// 0x38

        /// <summary></summary>
        MaxMessageSize = 57,	// 0x39

        /// <summary></summary>
        Vendor = 60,			// 0x3C

        /// <summary></summary>
        ClientIdentifier = 61,	// 0x3D

        /// <summary></summary>
        End = 255
    }

    /// <summary>DHCP可选项</summary>
    public class DhcpOption
    {
        #region 属性
        private DhcpOptions _Option;
        /// <summary>选项类型</summary>
        public DhcpOptions Option { get { return _Option; } set { _Option = value; } }

        private Byte _Length;
        /// <summary>长度</summary>
        public Byte Length { get { return _Length; } set { _Length = value; } }

        [FieldSize("_Length")]
        private Byte[] _Data;
        /// <summary>数据部分</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region 方法
        /// <summary>设置类型</summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public DhcpOption SetType(DhcpMessageType kind)
        {
            Option = DhcpOptions.MessageType;
            Length = 1;
            Data = new Byte[] { (Byte)kind };

            return this;
        }

        /// <summary>设置客户端标识</summary>
        /// <param name="clientid"></param>
        public void SetClientId(Byte[] clientid)
        {
            Option = DhcpOptions.ClientIdentifier;
            Length = (Byte)(1 + clientid.Length);
            Data = new Byte[Length];
            Data[0] = 1;
            Data.Write(1, clientid);
        }

        /// <summary>设置参数</summary>
        /// <param name="kind"></param>
        /// <param name="data"></param>
        public void SetData(DhcpOptions kind, Byte[] data)
        {
            Option = kind;
            Length = (Byte)data.Length;
            Data = data.ReadBytes();
        }
        #endregion

        #region 辅助
        /// <summary>转为字符串标识</summary>
        /// <returns></returns>
        public String ToStr()
        {
            switch (Option)
            {
                case DhcpOptions.Router:
                case DhcpOptions.Mask:
                case DhcpOptions.DNSServer:
                case DhcpOptions.DHCPServer:
                case DhcpOptions.RequestedIP:
                case DhcpOptions.NTPServer:
                case DhcpOptions.TimeServer:
                case DhcpOptions.NameServer:
                case DhcpOptions.LOGServer:
                    return new IPAddress(Data.ToInt()).ToString();
                case DhcpOptions.HostName:
                case DhcpOptions.Vendor:
                    return Data.ToStr();
                case DhcpOptions.MTU:
                    break;
                case DhcpOptions.StaticRout:
                    break;
                case DhcpOptions.ARPCacheTimeout:
                    break;
                case DhcpOptions.IPLeaseTime:
                    break;
                case DhcpOptions.MessageType:
                case DhcpOptions.Message:
                    return ((DhcpMessageType)Data[0]).ToString();
                case DhcpOptions.ParameterList:
                    break;
                case DhcpOptions.MaxMessageSize:
                    break;
                case DhcpOptions.ClientIdentifier:
                    return Data.ReadBytes(1, 6).ToHex(":");
                case DhcpOptions.End:
                    return "";
                default:
                    break;
            }

            return Data.ToHex();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Option + " " + ToStr();
        }
        #endregion
    }
}