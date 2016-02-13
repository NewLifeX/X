using System;
using System.Reflection;
using NewLife.Serialization;
using System.Linq;

namespace NewLife.Net.DNS
{
    /// <summary>DNS查询记录</summary>
    public class DNSQuery
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
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} {1}", Type, Name); }
        #endregion
    }

    /// <summary>DNS记录</summary>
    public class DNSRecord : DNSQuery, IMemberAccessor
    {
        #region 属性
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

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} {1}", Type, Text ?? Name); }
        #endregion

        #region IMemberAccessor 成员

        bool IMemberAccessor.Read(IFormatterX fm, MemberInfo member)
        {
            if (member.Name == "_Type")
            {
                Type = fm.Read<DNSQueryType>();

                // 如果是响应，创建具体消息
                //var de = fm.Hosts.Peek() as DNSEntity;
                var de = fm.Hosts.FirstOrDefault(e => e is DNSEntity) as DNSEntity;
                if (de != null && de.Header.Response)
                {
                    var dr = DNSEntity.CreateRecord(Type);
                    if (dr == null) return false;

                    dr.Name = Name;
                    dr.Type = Type;

                    // 设置给上级，让它用新的对象继续读取后面的成员
                    fm.Hosts.Pop();
                    fm.Hosts.Push(dr);
                }

                return true;
            }

            return false;
        }

        void IMemberAccessor.Write(IFormatterX fm, MemberInfo member)
        {
        }

        #endregion
    }
}