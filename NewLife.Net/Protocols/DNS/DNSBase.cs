using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>
    /// DNS实体类基类
    /// </summary>
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
            reader.Stream = stream;
            return reader.ReadObject(typeof(TEntity)) as TEntity;
        }
        #endregion
    }

    /// <summary>
    /// DNS实体类基类
    /// </summary>
    public abstract class DNSBase
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private DNSQueryType _Type;
        /// <summary>查询类型</summary>
        public DNSQueryType Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        private Int32 _TTL;
        /// <summary>生存时间</summary>
        public Int32 TTL
        {
            get { return _TTL; }
            set { _TTL = value; }
        }
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
        /// <summary>
        /// 把当前对象写入到数据流中去
        /// </summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            BinaryWriterX writer = new BinaryWriterX();
            writer.Stream = stream;
            writer.WriteObject(this);
        }
        #endregion
    }
}
