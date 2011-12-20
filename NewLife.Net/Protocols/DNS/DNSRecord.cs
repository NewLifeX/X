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
            reader.WriteLog("ReadMember", "_Name", "String", Name);

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
            writer.WriteLog("WriteMember", "_Name", "String", Name);

            var refs = writer.Items["Names"] as Dictionary<String, Int32>;
            if (refs == null) writer.Items.Add("Names", refs = new Dictionary<String, Int32>());
            WriteName(writer.Stream, Name, 0, refs);

            // 如果当前成员是_Questions，忽略三个字段
            AddFilter(writer);

            return false;
        }

        bool IAccessor.WriteComplete(IWriter writer, bool success)
        {
            RemoveFilter(writer);

            // 扩展数据里面可能有字符串引用
            var refs = writer.Items["Names"] as Dictionary<String, Int32>;
            if (refs == null) writer.Items.Add("Names", refs = new Dictionary<String, Int32>());
            if (!String.IsNullOrEmpty(DataString)) WriteAdditionalData(writer, refs);

            return success;
        }

        void AddFilter(IReaderWriter rw)
        {
            var ims = rw.Settings.IgnoreMembers;
            if (rw.CurrentMember != null && rw.CurrentMember.Name == "_Questions")
            {
                if (!ims.Contains("_TTL")) ims.Add("_TTL");
                if (!ims.Contains("_Length")) ims.Add("_Length");
                if (!ims.Contains("_Data")) ims.Add("_Data");
            }

            // 扩展数据自己写入
            if (rw is IWriter && !String.IsNullOrEmpty(DataString))
            {
                if (!ims.Contains("_Length")) ims.Add("_Length");
                if (!ims.Contains("_Data")) ims.Add("_Data");
            }
        }

        void RemoveFilter(IReaderWriter rw)
        {
            var ims = rw.Settings.IgnoreMembers;
            if (rw.CurrentMember != null && rw.CurrentMember.Name == "_Questions")
            {
                if (ims.Contains("_TTL")) ims.Remove("_TTL");
                if (ims.Contains("_Length")) ims.Remove("_Length");
                if (ims.Contains("_Data")) ims.Remove("_Data");
            }

            // 扩展数据自己写入
            if (rw is IWriter && !String.IsNullOrEmpty(DataString))
            {
                if (ims.Contains("_Length")) ims.Remove("_Length");
                if (ims.Contains("_Data")) ims.Remove("_Data");
            }
        }

        /// <summary>读取完成后，处理扩展数据</summary>
        /// <param name="reader"></param>
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

            reader.WriteLog("ReadMember", "_Data", "String", DataString);
        }

        /// <summary>写入的最后，处理扩展数据</summary>
        /// <param name="writer"></param>
        /// <param name="refs"></param>
        protected virtual void WriteAdditionalData(IWriter writer, Dictionary<String, Int32> refs)
        {
            writer.WriteLog("WriteMember", "_Data", "String", DataString);

            if (Type == DNSQueryType.NS)
            {
                var ms = new MemoryStream();
                // 传入当前流偏移，加2是因为待会要先写2个字节的长度
                WriteName(ms, DataString, writer.Stream.Position + 2, refs);
                Data = ms.ToArray();
            }
            else if (Type == DNSQueryType.A)
            {
                Data = IPAddress.Parse(DataString).GetAddressBytes();
            }

            // 写入数据
            _Length = 0;
            if (Data == null)
                writer.Write(_Length);
            else
            {
                _Length = (Int16)Data.Length;
                writer.Write(_Length);
                writer.Write(Data, 0, Data.Length);
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

        internal static void WriteName(Stream stream, String value, Int64 offset, Dictionary<String, Int32> refs)
        {
            // 有可能整个匹配
            Int32 p = 0;
            if (refs.TryGetValue("" + value, out p))
            {
                stream.WriteByte(0xC0);
                stream.WriteByte((Byte)p);
                return;
            }

            String[] ss = ("" + value).Split(".");
            var keys = new List<Int32>();
            var values = new List<String>();
            for (int i = 0; i < ss.Length; i++)
            {
                // 如果已存在，则写引用
                String name = String.Join(".", ss, i, ss.Length - i);
                if (refs.TryGetValue(name, out p))
                {
                    stream.WriteByte(0xC0);
                    stream.WriteByte((Byte)p);

                    for (int j = 0; j < keys.Count; j++)
                    {
                        refs.Add(values[j] + "." + name, keys[j]);
                    }

                    // 使用引用的必然是最后一个
                    return;
                }

                {
                    // 否则，先写长度，后存入引用
                    p = (Int32)stream.Position;

                    Byte[] buffer = Encoding.UTF8.GetBytes(ss[i]);
                    stream.WriteByte((Byte)buffer.Length);
                    stream.Write(buffer, 0, buffer.Length);

                    // 之前的每个加上str
                    for (int j = 0; j < values.Count; j++)
                    {
                        values[j] += "." + ss[i];
                    }

                    // 加入当前项
                    keys.Add((Int32)(offset + p));
                    values.Add(ss[i]);
                }
            }
            stream.WriteByte((Byte)0);

            for (int i = 0; i < keys.Count; i++)
            {
                refs.Add(values[i], keys[i]);
            }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} {1} {2} {3} {4}", Type, Class, Name, DataString, TTL); }
        #endregion
    }
}