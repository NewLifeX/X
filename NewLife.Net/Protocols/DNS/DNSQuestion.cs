using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Protocols.DNS
{
    class DNSQuestion
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
    }
}
