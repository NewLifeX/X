using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Collections;
using NewLife.Serialization;

namespace NewLife.Net.DNS
{
    /// <summary>DNS记录</summary>
    public class DNSRecord : IAccessor
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private DNSQueryType _Type = DNSQueryType.A;
        /// <summary>查询类型</summary>
        public virtual DNSQueryType Type { get { return _Type; } set { _Type = value; } }

        private DNSQueryClass _Class = DNSQueryClass.IN;
        /// <summary>协议组</summary>
        public DNSQueryClass Class { get { return _Class; } set { _Class = value; } }

        //private Int32 _TTL;
        ///// <summary>生存时间。4字节，指示RDATA中的资源记录在缓存的生存时间。</summary>
        //public TimeSpan TTL { get { return new TimeSpan(0, 0, _TTL); } set { _TTL = (Int32)value.TotalSeconds; } }

        private TimeSpan _TTL;
        /// <summary>生存时间。4字节，指示RDATA中的资源记录在缓存的生存时间。</summary>
        public TimeSpan TTL { get { return _TTL; } set { _TTL = value; } }

        private Int16 _Length;

        //[FieldSize("_Length")]
        //private Byte[] _Data;
        ///// <summary>资源数据</summary>
        //public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region IAccessor 成员
        bool IAccessor.Read(IReader reader)
        {
            // 如果当前成员是_Questions，忽略三个字段
            AddFilter(reader);

            // 在_Length读取完成后，读取资源数据，然后切换数据流
            reader.OnMemberReaded += new EventHandler<ReadMemberEventArgs>(reader_OnMemberReaded);

            return false;
        }

        void reader_OnMemberReaded(object sender, ReadMemberEventArgs e)
        {
            if (e.Member.Name == "_Length" && (Int16)e.Value > 0)
            {
                var reader = sender as IReader;
                // 记住数据流位置，读取字符串的时候需要用到
                reader.Items["Position"] = reader.Stream.Position;
                var data = reader.ReadBytes((Int16)e.Value);
                // 切换数据流，使用新数据流完成余下字段的序列化
                reader.Backup();
                reader.Stream = new MemoryStream(data);
            }
        }

        bool IAccessor.ReadComplete(IReader reader, bool success)
        {
            RemoveFilter(reader);

            // 恢复环境
            if (_Length > 0)
            {
                reader.Restore();
                if (reader.Items.Contains("Position")) reader.Items.Remove("Position");
            }
            reader.OnMemberReaded -= new EventHandler<ReadMemberEventArgs>(reader_OnMemberReaded);

            return success;
        }

        bool IAccessor.Write(IWriter writer)
        {
            // 如果当前成员是_Questions，忽略三个字段
            AddFilter(writer);

            return false;
        }

        bool IAccessor.WriteComplete(IWriter writer, bool success)
        {
            RemoveFilter(writer);

            return success;
        }

        static ICollection<String> fix = new HashSet<String>(StringComparer.OrdinalIgnoreCase) { "_Name", "_Type", "_Class" };
        void AddFilter(IReaderWriter rw)
        {
            var ims = rw.Settings.IgnoreMembers;
            if (rw.CurrentMember != null && rw.CurrentMember.Name == "_Questions")
            {
                //if (!ims.Contains("_TTL")) ims.Add("_TTL");
                //if (!ims.Contains("_Length")) ims.Add("_Length");
                //if (!ims.Contains("_Data")) ims.Add("_Data");

                // 只保留Name/Type/Class
                foreach (var item in rw.GetMembers(null, this))
                {
                    if (!fix.Contains(item.Name) && !ims.Contains(item.Name)) ims.Add(item.Name);
                }
            }
        }

        void RemoveFilter(IReaderWriter rw)
        {
            var ims = rw.Settings.IgnoreMembers;
            if (rw.CurrentMember != null && rw.CurrentMember.Name == "_Questions")
            {
                //if (ims.Contains("_TTL")) ims.Remove("_TTL");
                //if (ims.Contains("_Length")) ims.Remove("_Length");
                //if (ims.Contains("_Data")) ims.Remove("_Data");

                // 只保留Name/Type/Class
                foreach (var item in ObjectInfo.GetMembers(null, this, rw.Settings.UseField, rw.Settings.IsBaseFirst))
                {
                    if (!fix.Contains(item.Name) && ims.Contains(item.Name)) ims.Remove(item.Name);
                }
            }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} {1}", Type, Name); }
        #endregion

        #region 克隆
        /// <summary>克隆</summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public DNSRecord CloneFrom(DNSRecord dr)
        {
            Name = dr.Name;
            Type = dr.Type;
            Class = dr.Class;
            TTL = dr.TTL;
            _Length = dr._Length;

            return this;
        }
        #endregion
    }
}