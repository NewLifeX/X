using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}