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

        private TimeSpan _TTL;
        /// <summary>生存时间。4字节，指示RDATA中的资源记录在缓存的生存时间。</summary>
        public TimeSpan TTL { get { return _TTL; } set { _TTL = value; } }

        /// <summary>长度</summary>
        /// <remarks>后面应该是一个数据区域，留给派生类</remarks>
        private Int16 _Length;

        //[FieldSize("_Length")]
        //private Byte[] _Data;
        ///// <summary>资源数据</summary>
        //public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region 扩展属性
        [NonSerialized]
        private String _Text;
        /// <summary>文本信息</summary>
        public virtual String Text { get { return _Text; } set { _Text = value; } }
        #endregion

        #region 静态
        const String LENGTH = "_Length";
        const String POSITION = "Position";
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
            /*
             * 这里是设计上的一个难点，也是一个亮点！
             * 按照DNS协议，长度之后应该是一个由该长度指定的数据区域，
             * 如果我们按照一般二进制的做法，这里应该为这个数据区域专门定义多个不同的实体类，
             * DNS记录的DNSQueryType可以确定应该采用哪个实体类来进行读取。
             * 但是这样就让实体类变得非常难用了，难道还需要dr.Body.Address？不如直接的dr.Address来得好！
             * 
             * 因此，读写器需要支持备份恢复机制，我们在中途更换了数据流，让后续的读取按照新数据流读取。
             * 
             * 不过话说回来，如果仅仅因为这个目的，也是不需要切换数据流的，好像是因为字符串！
             * DNS字符串采用了一种很恶心的避免冗余的方式，它指定的位移是相对于局部数据区域的位移，所以必须更换数据流，让其以为从0开始。
             * 
             * 谨记：DNSRecord只是基类，拥有众多派生类，各自的扩展字段都不一样！
             */
            if (e.Member.Name == LENGTH && (Int16)e.Value > 0)
            {
                var reader = sender as IReader2;
                // 记住数据流位置，读取字符串的时候需要用到
                reader.Items[POSITION] = reader.Stream.Position;
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
                if (reader.Items.Contains(POSITION)) reader.Items.Remove(POSITION);
            }
            reader.OnMemberReaded -= new EventHandler<ReadMemberEventArgs>(reader_OnMemberReaded);

            return success;
        }

        bool IAccessor.Write(IWriter writer)
        {
            // 如果当前成员是_Questions，忽略三个字段
            AddFilter(writer);

            // 写_Length之前，切换数据流
            writer.OnMemberWriting += new EventHandler<WriteMemberEventArgs>(writer_OnMemberWriting);

            return false;
        }

        void writer_OnMemberWriting(object sender, WriteMemberEventArgs e)
        {
            if (e.Name == LENGTH)
            {
                var writer = sender as IWriter;
                // 记住数据流位置，读取字符串的时候需要用到
                // +2是为了先对长度进行占位，这个位置会影响DNS字符串的相对位置
                writer.Items[POSITION] = writer.Stream.Position + 2;
                // 切换数据流，使用新数据流完成余下字段的序列化
                writer.Backup();
                writer.Stream = new MemoryStream();


                e.Success = true;
            }
        }

        bool IAccessor.WriteComplete(IWriter writer, bool success)
        {
            // 先处理这个再移除过滤，因为这里需要依据被过滤的项来判断
            if (!writer.Settings.IgnoreMembers.Contains(LENGTH))
            {
                // 附加数据
                var ms = writer.Stream;
                ms.Position = 0;
                _Length = (Int16)ms.Length;

                // 恢复环境
                writer.Restore();
                if (writer.Items.Contains(POSITION)) writer.Items.Remove(POSITION);

                var wr = writer as IWriter2;
                // 写入_Length和数据
                wr.Write(_Length);
                var buffer = ms.ReadBytes();
                wr.Write(buffer, 0, buffer.Length);
            }

            writer.OnMemberWriting -= new EventHandler<WriteMemberEventArgs>(writer_OnMemberWriting);

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
                    if (!String.IsNullOrEmpty(item.Name) && !fix.Contains(item.Name) && !ims.Contains(item.Name)) ims.Add(item.Name);
                }
            }
        }

        void RemoveFilter(IReaderWriter rw)
        {
            var ims = rw.Settings.IgnoreMembers;
            if (ims.Count > 0 && rw.CurrentMember != null && rw.CurrentMember.Name == "_Questions")
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