using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>
    /// A记录
    /// </summary>
    public class DNS_A : DNSBase<DNS_A>
    {
        #region 属性
        [NonSerialized]
        private IPAddress _IP;
        /// <summary>IP地址</summary>
        public IPAddress IP
        {
            get { return _IP; }
            set { _IP = value; }
        }
        #endregion

        #region 构造
        ///// <summary>
        ///// 构造
        ///// </summary>
        //public DNS_A() { Type = DNSQueryType.A; }
        #endregion
    }
}