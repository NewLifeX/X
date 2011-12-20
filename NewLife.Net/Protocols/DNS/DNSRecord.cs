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

        private DNSQueryType _Type = DNSQueryType.A;
        /// <summary>属性说明</summary>
        public DNSQueryType Type { get { return _Type; } set { _Type = value; } }

        private DNSQueryClass _Class = DNSQueryClass.IN;
        /// <summary>属性说明</summary>
        public DNSQueryClass Class { get { return _Class; } set { _Class = value; } }

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
            //var refs = reader.Items["Names"] as Dictionary<Int32, String>;
            //if (refs == null) reader.Items.Add("Names", refs = new Dictionary<Int32, String>());
            //Name = ReadName(reader.Stream, 0, refs);
            Name = GetNameAccessor(reader).Read(reader.Stream, 0);
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

            //var refs = writer.Items["Names"] as Dictionary<String, Int32>;
            //if (refs == null) writer.Items.Add("Names", refs = new Dictionary<String, Int32>());
            //WriteName(writer.Stream, Name, 0, refs);
            GetNameAccessor(writer).Write(writer.Stream, Name, 0);

            // 如果当前成员是_Questions，忽略三个字段
            AddFilter(writer);

            return false;
        }

        bool IAccessor.WriteComplete(IWriter writer, bool success)
        {
            RemoveFilter(writer);

            // 扩展数据里面可能有字符串引用
            if (!String.IsNullOrEmpty(DataString)) WriteAdditionalData(writer);

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
            if (Type == DNSQueryType.NS || Type == DNSQueryType.CNAME)
            {
                // 当前指针在数据流后面
                DataString = GetNameAccessor(reader).Read(new MemoryStream(Data), reader.Stream.Position - _Length);
            }
            else if (Type == DNSQueryType.A)
            {
                DataString = new IPAddress(Data).ToString();
            }

            reader.WriteLog("ReadMember", "_Data", "String", DataString);
        }

        /// <summary>写入的最后，处理扩展数据</summary>
        /// <param name="writer"></param>
        protected virtual void WriteAdditionalData(IWriter writer)
        {
            writer.WriteLog("WriteMember", "_Data", "String", DataString);

            if (Type == DNSQueryType.NS || Type == DNSQueryType.CNAME)
            {
                var ms = new MemoryStream();
                // 传入当前流偏移，加2是因为待会要先写2个字节的长度
                //WriteName(ms, DataString, writer.Stream.Position + 2, refs);
                GetNameAccessor(writer).Write(ms, DataString, writer.Stream.Position + 2);
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

        static DNSNameAccessor GetNameAccessor(IReaderWriter rw)
        {
            var accessor = rw.Items["Names"] as DNSNameAccessor;
            if (accessor == null) rw.Items.Add("Names", accessor = new DNSNameAccessor());

            return accessor;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} {1} {2} {3} {4}", Type, Class, Name, DataString, TTL); }
        #endregion
    }
}