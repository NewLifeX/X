using System;
using System.Collections.Generic;
using System.Text;
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

        public override bool Write(object value, Type type)
        {
            _binary.Host = this.Host;

            // TXT记录的Text字段不采用DNS字符串
            if (type == typeof(String) && Host.Member.Name != "_Text")
            {
                //var ps = Host.Items["Position"];
                //Int64 p = ps is Int64 ? (Int64)ps : 0;
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

        public override bool TryRead(Type type, ref object value)
        {
            _binary.Host = this.Host;

            // TXT记录的Text字段不采用DNS字符串
            if (type == typeof(String) && Host.Member.Name != "_Text")
            {
                //var ps = Host.Items["Position"];
                //Int64 p = ps is Int64 ? (Int64)ps : 0;
                Int64 p = 0;
                value = Accessor.Read(Host.Stream, p);

                return true;
            }
            if (type == typeof(TimeSpan))
            {
                value = new TimeSpan(0, 0, Host.Read<Int32>());

                return true;
            }
            //// 如果是DNSRecord，这里需要处理一下，变为真正的记录类型
            //if (type == typeof(DNSRecord))
            //{
            //    var p = Host.Stream.Position;

            //    //var name = Accessor.Read(Host.Stream, 0);
            //    //var qt = Host.Read<DNSQueryType>();
            //    //var qc = Host.Read<DNSQueryClass>();
            //    //var dr = new DNSRecord();
            //    //value = dr;
            //    //if (!_binary.TryRead(type, ref value)) return false;

            //    // 如果是请求，到此结束
            //    var de = Host.Hosts.Peek() as DNSEntity;
            //    var isReq = de != null && !de.Header.Response;
            //    if (isReq)
            //    {
            //        var dr = new DNSRecord();
            //        //dr.Name = name;
            //        //dr.Type = qt;
            //        //dr.Class = qc;
            //        value = dr;

            //        _binary.IgnoreMembers.Add("_TTL");
            //        _binary.IgnoreMembers.Add("_Length");
            //        if (!_binary.TryRead(type, ref value)) return false;

            //        return true;
            //    }

            //    // 退回去，让序列化自己读
            //    Host.Stream.Position = p;

            //    var dr2 = DNSEntity.CreateRecord(qt);
            //    if (dr2 != null)
            //    {
            //        type = dr2.GetType();
            //        //dr2.Name = dr.Name;
            //        //dr2.Type = dr.Type;
            //        value = dr2;
            //    }

            //    _binary.IgnoreMembers.Add("_TTL");
            //    _binary.IgnoreMembers.Add("_Length");
                
            //    return _binary.TryRead(type, ref value);
            //}

            return false;
        }
    }

    /// <summary>DNS字符串的二进制序列化</summary>
    /// <remarks>
    /// DNS字符串的存取访问比较特殊
    /// </remarks>
    class BinaryDNSString : BinaryHandlerBase
    {
        public DNSNameAccessor Accessor { get; set; }

        public BinaryDNSString()
        {
            // 提高优先级，必须在基础处理器之前
            Priority = 0;
        }

        public override bool Write(object value, Type type)
        {
            // TXT记录的Text字段不采用DNS字符串
            if (type == typeof(String) && Host.Member.Name != "_Text")
            {
                //var ps = Host.Items["Position"];
                //Int64 p = ps is Int64 ? (Int64)ps : 0;
                Int64 p = 0;
                p += Host.Stream.Position;
                Accessor.Write(Host.Stream, (String)value, p);

                return true;
            }

            return false;
        }

        public override bool TryRead(Type type, ref object value)
        {
            // TXT记录的Text字段不采用DNS字符串
            if (type == typeof(String) && Host.Member.Name != "_Text")
            {
                //var ps = Host.Items["Position"];
                //Int64 p = ps is Int64 ? (Int64)ps : 0;
                Int64 p = 0;
                value = Accessor.Read(Host.Stream, p);

                return true;
            }

            return false;
        }
    }
}