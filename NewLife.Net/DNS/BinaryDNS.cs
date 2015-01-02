using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.DNS
{
    /// <summary>DNS的二进制序列化</summary>
    class BinaryDNS : BinaryHandlerBase
    {
        BinaryComposite _binary = new BinaryComposite();
        DNSNameAccessor accessor = new DNSNameAccessor();

        public BinaryDNS()
        {
            Priority = 90;
        }

        public override bool Write(object value, Type type)
        {
            _binary.Host = this.Host;

            // TXT记录的Text字段不采用DNS字符串
            //if (type == typeof(String) && Host.Member.Name != "_Text")
            //{
            //    var ps = Host.Items["Position"];
            //    Int64 p = ps is Int64 ? (Int64)ps : 0;
            //    p += Host.Stream.Position;
            //    accessor.Write(Host.Stream, (String)value, p);

            //    return true;
            //}
            //else
            if (type == typeof(TimeSpan))
            {
                var ts = (TimeSpan)value;
                Host.Write((Int32)ts.TotalSeconds);

                return true;
            }
            else
                return false;

            //return _binary.Write(value, type);
        }

        public override bool TryRead(Type type, ref object value)
        {
            _binary.Host = this.Host;

            // TXT记录的Text字段不采用DNS字符串
            //if (type == typeof(String) && Host.Member.Name != "_Text")
            //{
            //    var ps = Host.Items["Position"];
            //    Int64 p = ps is Int64 ? (Int64)ps : 0;
            //    value = accessor.Read(Host.Stream, p);

            //    return true;
            //}
            //else 
            if (type == typeof(TimeSpan))
            {
                value = new TimeSpan(0, 0, Host.Read<Int32>());

                return true;
            }
            // 如果是DNSRecord，这里需要处理一下，变为真正的记录类型
            else if (type == typeof(DNSRecord))
            {
                var p = Host.Stream.Position;
                var name = accessor.Read(Host.Stream, 0);
                var qt = Host.Read<DNSQueryType>();
                // 退回去，让序列化自己读
                Host.Stream.Position = p;

                var dr = DNSEntity.CreateRecord(qt);
                if (dr != null)
                    type = dr.GetType();
                else
                    dr = new DNSRecord();
                dr.Name = name;

                value = dr;
            }
            else
                return false;

            return _binary.TryRead(type, ref value);
        }
    }
}