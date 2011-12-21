using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Serialization;
using NewLife.Net.Sockets;
using NewLife.Net;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>DNS实体类基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class DNSBase<TEntity> : DNSBase where TEntity : DNSBase<TEntity>
    {
        #region 读写
        /// <summary>
        /// 从数据流中读取对象
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static TEntity Read(Stream stream)
        {
            BinaryReaderX reader = new BinaryReaderX();
            reader.Settings.IsLittleEndian = false;
            reader.Settings.UseObjRef = false;
            reader.Stream = stream;
#if DEBUG
            if (NetHelper.Debug)
            {
                reader.Debug = true;
                reader.EnableTraceStream();
            }
#endif
            return reader.ReadObject(typeof(TEntity)) as TEntity;
        }
        #endregion
    }

    /// <summary>DNS实体类基类</summary>
    public abstract class DNSBase //: IAccessor
    {
        #region 属性
        private DNSHeader _Header = new DNSHeader();
        /// <summary>头部</summary>
        public DNSHeader Header { get { return _Header; } set { _Header = value; } }

        [FieldSize("_Header._Questions")]
        private DNSRecord[] _Questions;
        /// <summary>请求段</summary>
        public DNSRecord[] Questions { get { return _Questions; } set { _Questions = value; } }

        [FieldSize("_Header._Answers")]
        private DNSRecord[] _Answers;
        /// <summary>回答段</summary>
        public DNSRecord[] Answers { get { return _Answers; } set { _Answers = value; } }

        [FieldSize("_Header._Authorities")]
        private DNSRecord[] _Authoritis;
        /// <summary>授权段</summary>
        public DNSRecord[] Authoritis { get { return _Authoritis; } set { _Authoritis = value; } }

        [FieldSize("_Header._Additionals")]
        private DNSRecord[] _Additionals;
        /// <summary>附加段</summary>
        public DNSRecord[] Additionals { get { return _Additionals; } set { _Additionals = value; } }
        #endregion

        #region 构造
        ///// <summary>
        ///// 构造函数
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="type"></param>
        ///// <param name="ttl"></param>
        //public DNSBase(String name, DNSQueryType type, Int32 ttl)
        //{
        //    Name = name;
        //    Type = type;
        //    TTL = ttl;
        //}
        #endregion

        #region 读写
        /// <summary>把当前对象写入到数据流中去</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            BinaryWriterX writer = new BinaryWriterX();
            writer.Settings.IsLittleEndian = false;
            writer.Settings.UseObjRef = false;
            writer.Stream = stream;
#if DEBUG
            if (NetHelper.Debug)
            {
                writer.Debug = true;
                writer.EnableTraceStream();
            }
#endif
            writer.WriteObject(this);
        }
        #endregion

        #region IAccessor 成员
        //bool IAccessor.Read(IReader reader)
        //{
        //    return false;
        //}

        //bool IAccessor.ReadComplete(IReader reader, bool success) { return success; }

        //bool IAccessor.Write(IWriter writer)
        //{
        //    return false;
        //}

        //bool IAccessor.WriteComplete(IWriter writer, bool success) { return success; }
        #endregion
    }
}