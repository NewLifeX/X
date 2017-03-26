using System;
using NewLife.Serialization;

namespace NewLife.Net.DNS
{
    /// <summary>DNS的二进制序列化</summary>
    class BinaryDNS : BinaryHandlerBase
    {
        BinaryComposite _binary;
        public DNSNameAccessor Accessor { get; set; }

        public BinaryDNS()
        {
            //Priority = 90;
            // 提高优先级，必须在基础处理器之前
            Priority = 0;

            _binary = new BinaryComposite();
            Accessor = new DNSNameAccessor();
        }

        public override Boolean Write(Object value, Type type)
        {
            _binary.Host = Host;

            // TXT记录的Text字段不采用DNS字符串
            if (type == typeof(String) && Host.Member.Name != "_Text")
            {
                Int64 p = 0;
                p += Host.Stream.Position;
                Accessor.Write(Host.Stream, (String)value, p);

                return true;
            }
            if (type == typeof(TimeSpan))
            {
                var ts = (TimeSpan)value;
                Host.Write((Int32)ts.TotalSeconds);

                return true;
            }

            return false;
        }

        public override Boolean TryRead(Type type, ref Object value)
        {
            _binary.Host = Host;

            // TXT记录的Text字段不采用DNS字符串
            if (type == typeof(String) && Host.Member.Name != "_Text")
            {
                value = Accessor.Read(Host.Stream, 0);

                return true;
            }
            if (type == typeof(TimeSpan))
            {
                value = new TimeSpan(0, 0, Host.Read<Int32>());

                return true;
            }
  
            return false;
        }
    }
}