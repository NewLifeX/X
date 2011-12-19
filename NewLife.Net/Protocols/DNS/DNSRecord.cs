using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;
using System.IO;
using NewLife.IO;
using System.Net;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>资源结构</summary>
    public class DNSRecord : IAccessor
    {
        #region 属性
        [NonSerialized]
        private String _Name;
        /// <summary>属性说明</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private DNSQueryType _Type;
        /// <summary>属性说明</summary>
        public DNSQueryType Type { get { return _Type; } set { _Type = value; } }

        private QueryClass _Class;
        /// <summary>属性说明</summary>
        public QueryClass Class { get { return _Class; } set { _Class = value; } }

        private Int32 _TTL;
        /// <summary>生存时间。4字节，指示RDATA中的资源记录在缓存的生存时间。</summary>
        public TimeSpan TTL { get { return new TimeSpan(0, 0, _TTL); } set { _TTL = (Int32)value.TotalSeconds; } }

        private Int16 _Length;

        [FieldSize("_Length")]
        private Byte[] _Data;
        /// <summary>资源数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region 扩展数据
        [NonSerialized]
        private String _DataString;
        /// <summary>数据字符串</summary>
        public String DataString { get { return _DataString; } set { _DataString = value; } }
        #endregion

        #region IAccessor 成员

        bool IAccessor.Read(IReader reader)
        {
            // 提前读取名称
            var refs = reader.Items["Names"] as Dictionary<Int32, String>;
            if (refs == null) reader.Items.Add("Names", refs = new Dictionary<Int32, String>());
            Name = ReadName(reader.Stream, 0, refs);

            // 如果当前成员是_Questions，忽略三个字段
            AddFilter(reader);

            return false;
        }

        bool IAccessor.ReadComplete(IReader reader, bool success)
        {
            RemoveFilter(reader);

            // 扩展数据里面可能有字符串引用
            if (_Length > 0) ReadAdditionalData(reader);

            return success;
        }

        bool IAccessor.Write(IWriter writer)
        {
            // 提前写入名称
            var refs = writer.Items["Names"] as Dictionary<Int32, String>;
            if (refs == null) writer.Items.Add("Names", refs = new Dictionary<Int32, String>());
            WriteName(writer.Stream, Name, refs);

            // 扩展数据里面可能有字符串引用
            if (!String.IsNullOrEmpty(DataString)) WriteAdditionalData(writer, refs);

            // 如果当前成员是_Questions，忽略三个字段
            AddFilter(writer);

            return false;
        }

        bool IAccessor.WriteComplete(IWriter writer, bool success)
        {
            RemoveFilter(writer);
            return success;
        }

        static void AddFilter(IReaderWriter rw)
        {
            if (rw.CurrentMember != null && rw.CurrentMember.Name == "_Questions")
            {
                var ims = rw.Settings.IgnoreMembers;
                if (!ims.Contains("_TTL")) ims.Add("_TTL");
                if (!ims.Contains("_Length")) ims.Add("_Length");
                if (!ims.Contains("_Data")) ims.Add("_Data");
            }
        }

        static void RemoveFilter(IReaderWriter rw)
        {
            if (rw.CurrentMember != null && rw.CurrentMember.Name == "_Questions")
            {
                var ims = rw.Settings.IgnoreMembers;
                if (ims.Contains("_TTL")) ims.Remove("_TTL");
                if (ims.Contains("_Length")) ims.Remove("_Length");
                if (ims.Contains("_Data")) ims.Remove("_Data");
            }
        }

        protected virtual void ReadAdditionalData(IReader reader)
        {
            if (Type == DNSQueryType.NS)
            {
                var refs = reader.Items["Names"] as Dictionary<Int32, String>;
                if (refs == null) reader.Items.Add("Names", refs = new Dictionary<Int32, String>());
                DataString = ReadName(new MemoryStream(Data), reader.Stream.Position - _Length, refs);
            }
            else if (Type == DNSQueryType.A)
            {
                DataString = new IPAddress(Data).ToString();
            }
        }

        protected virtual void WriteAdditionalData(IWriter writer, Dictionary<Int32, String> refs)
        {
            if (Type == DNSQueryType.NS)
            {
                var ms = new MemoryStream();
                WriteName(ms, DataString, refs);
                Data = ms.ToArray();
            }
            else if (Type == DNSQueryType.A)
            {
                Data = IPAddress.Parse(DataString).GetAddressBytes();
            }
        }
        #endregion

        #region 处理名称
        internal static String ReadName(Stream stream, Int64 offset, Dictionary<Int32, String> refs)
        {
            Int64 p = stream.Position;
            Int32 n = 0;
            StringBuilder sb = new StringBuilder();
            var keys = new List<Int32>();
            var values = new List<String>();
            while ((n = stream.ReadByte()) != 0)
            {
                if (sb.Length > 0) sb.Append(".");

                String str = null;

                // 0xC0表示是引用，下一个地址指示偏移量
                if (n == 0xC0)
                {
                    var n2 = stream.ReadByte();
                    str = refs[n2];
                }
                else
                {
                    //if (stream.Position + n > stream.Length) return sb.ToString();

                    Byte[] buffer = stream.ReadBytes(n);
                    str = Encoding.UTF8.GetString(buffer);
                }

                sb.Append(str);

                // 之前的每个加上str
                for (int i = 0; i < values.Count; i++)
                {
                    values[i] += "." + str;
                }

                // 加入当前项
                keys.Add((Int32)(offset + p));
                values.Add(str);

                if (n == 0xC0) break;

                p = stream.Position;
            }
            for (int i = 0; i < keys.Count; i++)
            {
                refs.Add(keys[i], values[i]);
            }

            return sb.ToString();
        }

        internal static void WriteName(Stream stream, String value, Dictionary<Int32, String> refs)
        {
            String[] ss = ("" + value).Split(".");
            for (int i = 0; i < ss.Length; i++)
            {
                Byte[] buffer = Encoding.UTF8.GetBytes(ss[i]);
                stream.WriteByte((Byte)buffer.Length);
                stream.Write(buffer, 0, buffer.Length);
            }
            stream.WriteByte((Byte)0);
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} {1} {2} {3} {4}", Type, Class, Name, DataString, TTL); }
        #endregion
    }
}