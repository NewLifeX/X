using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.Protocols.DNS
{
    class DNSQuestion : IAccessor
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
        #endregion

        #region IAccessor 成员

        bool IAccessor.Read(IReader reader)
        {
            // 提前读取名称
            Name = ReadName(reader);

            return false;
        }

        bool IAccessor.ReadComplete(IReader reader, bool success) { return success; }

        bool IAccessor.Write(IWriter writer)
        {
            // 提前写入名称
            WriteName(writer, Name);

            return false;
        }

        bool IAccessor.WriteComplete(IWriter writer, bool success) { return success; }

        #endregion

        #region 处理名称
        internal static String ReadName(IReader reader)
        {
            Int32 n = 0;
            StringBuilder sb = new StringBuilder();
            while ((n = reader.ReadByte()) != 0)
            {
                if (sb.Length > 0) sb.Append(".");

                Byte[] buffer = reader.ReadBytes(n);
                sb.Append(Encoding.UTF8.GetString(buffer));
            }

            return sb.ToString();
        }

        internal static void WriteName(IWriter writer, String value)
        {
            String[] ss = ("" + value).Split(".");
            for (int i = 0; i < ss.Length; i++)
            {
                Byte[] buffer = Encoding.UTF8.GetBytes(ss[i]);
                writer.Write((Byte)buffer.Length);
                writer.Write(buffer, 0, buffer.Length);
            }
            writer.Write((Byte)0);
        }
        #endregion
    }
}
