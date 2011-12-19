using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

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
        /// <summary>属性说明</summary>
        public Int32 TTL { get { return _TTL; } set { _TTL = value; } }

        private Int32 _Length;

        [FieldSize("_Length")]
        private Byte[] _Data;
        /// <summary>资源数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region IAccessor 成员

        bool IAccessor.Read(IReader reader)
        {
            // 提前读取名称
            Name = ReadName(reader);

            // 如果当前成员是_Questions，忽略三个字段
            if (reader.CurrentMember != null && reader.CurrentMember.Name == "_Questions")
            {
                var ims = reader.Settings.IgnoreMembers;
                if (!ims.Contains("_TTL")) ims.Add("_TTL");
                if (!ims.Contains("_Length")) ims.Add("_Length");
                if (!ims.Contains("_Data")) ims.Add("_Data");
            }

            return false;
        }

        bool IAccessor.ReadComplete(IReader reader, bool success) { return success; }

        bool IAccessor.Write(IWriter writer)
        {
            // 提前写入名称
            WriteName(writer, Name);

            // 如果当前成员是_Questions，忽略三个字段
            if (writer.CurrentMember != null && writer.CurrentMember.Name == "_Questions")
            {
                var ims = writer.Settings.IgnoreMembers;
                if (!ims.Contains("_TTL")) ims.Add("_TTL");
                if (!ims.Contains("_Length")) ims.Add("_Length");
                if (!ims.Contains("_Data")) ims.Add("_Data");
            }

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

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} {1} {2}", Type, Class, Name); }
        #endregion
    }
}