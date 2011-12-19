using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>资源结构</summary>
    public class DNSResource
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
    }
}