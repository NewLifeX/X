using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.Dhcp
{
    /// <summary>DHCP实体</summary>
    public class DhcpEntity
    {
        #region 属性
        private Byte _MessageType;
        /// <summary>消息类型</summary>
        public Byte MessageType { get { return _MessageType; } set { _MessageType = value; } }

        private Byte _HardwareType;
        /// <summary>硬件类型</summary>
        public Byte HardwareType { get { return _HardwareType; } set { _HardwareType = value; } }

        private Byte _HardwareAddressLength;
        /// <summary>硬件地址长度</summary>
        public Byte HardwareAddressLength { get { return _HardwareAddressLength; } set { _HardwareAddressLength = value; } }

        private Byte _Hops;
        /// <summary>属性说明</summary>
        public Byte Hops { get { return _Hops; } set { _Hops = value; } }

        private Int32 _TransactionID;
        /// <summary>会话编号</summary>
        public Int32 TransactionID { get { return _TransactionID; } set { _TransactionID = value; } }

        private Int16 _Seconds;
        /// <summary>秒数</summary>
        public Int16 Seconds { get { return _Seconds; } set { _Seconds = value; } }

        private Int16 _BootpFlags;
        /// <summary>属性说明</summary>
        public Int16 BootpFlags { get { return _BootpFlags; } set { _BootpFlags = value; } }

        [FieldSize(4)]
        private Byte[] _ClientIPAddress;
        /// <summary>客户端IP地址</summary>
        public Byte[] ClientIPAddress { get { return _ClientIPAddress; } set { _ClientIPAddress = value; } }

        [FieldSize(4)]
        private Byte[] _YourIPAddress;
        /// <summary>属性说明</summary>
        public Byte[] YourIPAddress { get { return _YourIPAddress; } set { _YourIPAddress = value; } }

        [FieldSize(4)]
        private Byte[] _NextServerIPAddress;
        /// <summary>属性说明</summary>
        public Byte[] NextServerIPAddress { get { return _NextServerIPAddress; } set { _NextServerIPAddress = value; } }

        [FieldSize(4)]
        private Byte[] _RelayAgentIPAddress;
        /// <summary>属性说明</summary>
        public Byte[] RelayAgentIPAddress { get { return _RelayAgentIPAddress; } set { _RelayAgentIPAddress = value; } }

        [FieldSize(16)]
        private Byte[] _ClientMACAddress;
        /// <summary>客户端MAC地址。占用16字节，实际内存长度由_HardwareAddressLength决定</summary>
        public Byte[] ClientMACAddress { get { return _ClientMACAddress; } set { _ClientMACAddress = value; } }

        [FieldSize(64)]
        private String _ServerName;
        /// <summary>服务器名</summary>
        public String ServerName { get { return _ServerName; } set { _ServerName = value; } }

        [FieldSize(128)]
        private String _BootFileName;
        /// <summary>启动文件名</summary>
        public String BootFileName { get { return _BootFileName; } set { _BootFileName = value; } }
        #endregion
    }
}