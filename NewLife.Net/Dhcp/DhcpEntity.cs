using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NewLife.Messaging;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Net.Dhcp
{
    /// <summary>DHCP实体</summary>
    public class DhcpEntity : MessageBase
    {
        #region 属性
        private Byte _MessageType = 1;
        /// <summary>消息类型。若是client送给server的封包，设为1，反向为2</summary>
        public Byte MessageType { get { return _MessageType; } set { _MessageType = value; } }

        private Byte _HardwareType = 1;
        /// <summary>硬件类型</summary>
        public Byte HardwareType { get { return _HardwareType; } set { _HardwareType = value; } }

        private Byte _HardwareAddressLength = 6;
        /// <summary>硬件地址长度</summary>
        public Byte HardAddrLength { get { return _HardwareAddressLength; } set { _HardwareAddressLength = value; } }

        private Byte _Hops;
        /// <summary>属性说明</summary>
        public Byte Hops { get { return _Hops; } set { _Hops = value; } }

        private Int32 _TransactionID;
        /// <summary>会话编号</summary>
        public Int32 TransactionID { get { return _TransactionID; } set { _TransactionID = value; } }

        private Int16 _Seconds;
        /// <summary>秒数</summary>
        public Int16 Seconds { get { return _Seconds; } set { _Seconds = value; } }

        private UInt16 _BootpFlags = 0x8000;
        /// <summary>属性说明</summary>
        public UInt16 BootpFlags { get { return _BootpFlags; } set { _BootpFlags = value; } }

        [FieldSize(4)]
        private Byte[] _ClientIPAddress;
        /// <summary>客户端IP地址</summary>
        public Byte[] ClientIP { get { return _ClientIPAddress; } set { _ClientIPAddress = value; } }

        [FieldSize(4)]
        private Byte[] _YourIPAddress;
        /// <summary>属性说明</summary>
        public Byte[] YourIP { get { return _YourIPAddress; } set { _YourIPAddress = value; } }

        [FieldSize(4)]
        private Byte[] _NextServerIPAddress;
        /// <summary>属性说明</summary>
        public Byte[] NextServerIP { get { return _NextServerIPAddress; } set { _NextServerIPAddress = value; } }

        [FieldSize(4)]
        private Byte[] _RelayAgentIPAddress;
        /// <summary>属性说明</summary>
        public Byte[] RelayAgentIP { get { return _RelayAgentIPAddress; } set { _RelayAgentIPAddress = value; } }

        [FieldSize(16)]
        private Byte[] _ClientMACAddress;
        /// <summary>客户端MAC地址。占用16字节，实际内存长度由_HardwareAddressLength决定</summary>
        public Byte[] ClientMac { get { return _ClientMACAddress; } set { _ClientMACAddress = value; } }

        [FieldSize(64)]
        private String _ServerName;
        /// <summary>服务器名</summary>
        public String ServerName { get { return _ServerName; } set { _ServerName = value; } }

        [FieldSize(128)]
        private String _BootFileName;
        /// <summary>启动文件名</summary>
        public String BootFileName { get { return _BootFileName; } set { _BootFileName = value; } }

        private Int32 _Magic = 0x63825363;
        /// <summary>幻数</summary>
        public Int32 Magic { get { return _Magic; } set { _Magic = value; } }
        #endregion

        #region 扩展属性
        [NonSerialized]
        private List<DhcpOption> _Options = new List<DhcpOption>();
        /// <summary>可选项</summary>
        public List<DhcpOption> Options { get { return _Options; } set { _Options = value; } }

        /// <summary>获取可选项</summary>
        /// <param name="type"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        public DhcpOption Get(DhcpOptions type, Boolean add = false)
        {
            var opt = Options.FirstOrDefault(e => e.Option == type);
            if (opt == null && add)
            {
                opt = new DhcpOption();
                Options.Add(opt);
            }
            return opt;
        }

        /// <summary>消息种类</summary>
        [XmlIgnore]
        public Dhcp.DhcpMessageType Kind
        {
            get
            {
                var opt = Get(DhcpOptions.MessageType);
                var b = opt != null ? opt.Data[0] : 0;
                return (DhcpMessageType)b;
            }
            set
            {
                var opt = Get(DhcpOptions.MessageType, true);
                opt.SetType(value);
            }
        }
        #endregion

        #region 读写核心
        /// <summary>读取扩展属性</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public override bool Read(Stream stream)
        {
            if (!base.Read(stream)) return false;

            // 读取扩展可选项
            if (stream.Position < stream.Length)
            {
                var binary = GetFormatter(true) as Binary;
                binary.Stream = stream;

                while (stream.Position < stream.Length)
                {
                    if (stream.ReadByte() == 0xFF) break;
                    stream.Position--;

                    var opt = binary.Read<DhcpOption>();
                    Options.Add(opt);

                    if (opt.Option == DhcpOptions.End) break;
                }
            }

            return true;
        }

        /// <summary>写入可选项</summary>
        /// <param name="stream"></param>
        public override void Write(Stream stream)
        {
            base.Write(stream);

            var binary = GetFormatter(true) as Binary;
            binary.Stream = stream;
            foreach (var opt in Options)
            {
                if (opt.Option == DhcpOptions.End) break;

                binary.Write(opt);
            }

            // 写入可选项结束符
            stream.Write(0xFF);
        }

        /// <summary>使用字段大小</summary>
        /// <param name="isRead"></param>
        /// <returns></returns>
        protected override IFormatterX GetFormatter(bool isRead)
        {
            var fm = base.GetFormatter(isRead) as Binary;
            fm.UseFieldSize = true;
            //fm.Log = XTrace.Log;

            return fm;
        }
        #endregion

        #region 辅助
        /// <summary>显示扩展属性</summary>
        /// <param name="pi"></param>
        /// <param name="len"></param>
        /// <param name="sb"></param>
        protected override void GetMember(PropertyInfo pi, int len, StringBuilder sb)
        {
            if (pi.Name == "Options")
            {
                foreach (var opt in Options)
                {
                    if (sb.Length > 0) sb.AppendLine();

                    sb.AppendFormat("{0," + len + "}: {1}", opt.Option, opt.ToStr());
                }
                return;
            }

            base.GetMember(pi, len, sb);
        }

        /// <summary>获取用于输出的成员值</summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        protected override object GetMemberValue(PropertyInfo pi)
        {
            var v = base.GetMemberValue(pi);
            var type = pi.PropertyType;

            if (type.IsEnum) return v;

            if (pi.Name.Contains("IP")) return new IPAddress(v.ToInt());
            if (pi.Name.Contains("Mac"))
            {
                var buf = (Byte[])this.GetValue(pi);
                if (buf == null) return null;
                return buf.ReadBytes(0, HardAddrLength).ToHex(":");
            }

            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Byte:
                    return "0x{0:X2}".F(v.ToInt());
                case TypeCode.DateTime:
                    return "{0}".F(v);
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return "0x{0:X4}".F(v);
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return "0x{0:X8}".F(v);
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return "0x{0:X16}".F(v);
            }

            return v;
        }
        #endregion
    }
}