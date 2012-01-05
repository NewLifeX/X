using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;
using System.Net;

namespace NewLife.Net.Stun
{
    class StunAttribute
    {
        #region 属性
        /* RFC 3489 11.2.
            Each attribute is TLV encoded, with a 16 bit type, 16 bit length, and variable value:

            0                   1                   2                   3
            0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            |         Type                  |            Length             |
            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            |                             Value                             ....
            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                            
        */

        //[NonSerialized]
        //private AttributeType _Type;
        ///// <summary>类型</summary>
        //public AttributeType Type { get { return _Type; } set { _Type = value; } }

        private Int16 _Length;
        /// <summary>属性说明</summary>
        public Int16 Length { get { return _Length; } set { _Length = value; } }

        [FieldSize("_Length")]
        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region 扩展属性
        /// <summary>网络节点</summary>
        public IPEndPoint EndPoint
        {
            get { return Data == null ? null : ParseEndPoint(Data); }
            set { Data = StoreEndPoint(value); }
        }

        /// <summary>字符串</summary>
        public String Str
        {
            get
            {
                if (Data == null) return null;
                return Encoding.UTF8.GetString(Data);
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    Data = null;
                else
                    Data = Encoding.UTF8.GetBytes(value);
            }
        }

        /// <summary>整型</summary>
        public Int32 Int
        {
            get
            {
                if (Data == null) return 0;
                var d = new Byte[4];
                Data.CopyTo(d, 0);
                Array.Reverse(d);
                return BitConverter.ToInt32(d, 0);
            }
            set
            {
                var d = BitConverter.GetBytes(value);
                Array.Reverse(d);
                Data = d;
            }
        }

        public T GetValue<T>()
        {
            if (Data == null || Data.Length < 1) return default(T);

            Object value = null;
            Type t = typeof(T);
            if (t == typeof(IPEndPoint))
                value = EndPoint;
            else if (t == typeof(String))
                value = Str;
            else if (t == typeof(Int32))
                value = Int;

            return (T)value;
        }

        public void SetValue<T>(T value)
        {
            Object v = value;
            Type t = typeof(T);
            if (t == typeof(IPEndPoint))
                EndPoint = (IPEndPoint)v;
            else if (t == typeof(String))
                Str = (String)v;
            else if (t == typeof(Int32))
                Int = (Int32)v;
        }
        #endregion

        #region 分析终结点
        IPEndPoint ParseEndPoint(Byte[] data)
        {
            /*
                It consists of an eight bit address family, and a sixteen bit
                port, followed by a fixed length value representing the IP address.

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |x x x x x x x x|    Family     |           Port                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                             Address                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            */

            // Skip family
            //Int32 p = 2;

            // Port
            var arr = new Byte[2];
            Array.Copy(data, 2, arr, 0, arr.Length);
            Array.Reverse(arr);
            var port = BitConverter.ToUInt16(arr, 0);

            // Address
            Byte[] ip = new Byte[4];
            //data.CopyTo(ip, 4);
            Array.Copy(data, 4, ip, 0, ip.Length);

            return new IPEndPoint(new IPAddress(ip), port);
        }

        Byte[] StoreEndPoint(IPEndPoint endPoint)
        {
            /*
                It consists of an eight bit address family, and a sixteen bit
                port, followed by a fixed length value representing the IP address.

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |x x x x x x x x|    Family     |           Port                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                             Address                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+             
            */

            if (endPoint == null) return null;

            //// 头部，类型和长度（固定8）
            //writer.Write((Int16)type);
            //writer.Write((Int16)8);

            //writer.Write((Byte)1);
            //writer.Write(endPoint.Port);
            //writer.Write(endPoint.Address.GetAddressBytes());

            var data = new Byte[8];
            data[1] = 1;

            var d = BitConverter.GetBytes((UInt16)endPoint.Port);
            Array.Reverse(d);
            d.CopyTo(data, 2);

            endPoint.Address.GetAddressBytes().CopyTo(data, 4);

            return data;
        }
        #endregion
    }

    /// <summary>属性类型</summary>
    enum AttributeType : ushort
    {
        MappedAddress = 0x0001,
        ResponseAddress = 0x0002,
        ChangeRequest = 0x0003,
        SourceAddress = 0x0004,
        ChangedAddress = 0x0005,
        Username = 0x0006,
        Password = 0x0007,
        MessageIntegrity = 0x0008,
        ErrorCode = 0x0009,
        UnknownAttribute = 0x000A,
        ReflectedFrom = 0x000B,
        XorMappedAddress = 0x8020,
        XorOnly = 0x0021,
        ServerName = 0x8022,
    }
}