using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using NewLife.Serialization;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>PTR记录</summary>
    public class DNS_PTR : DNSBase<DNS_PTR>
    {
        #region 属性
        private String _DomainName;
        /// <summary>域名</summary>
        public String DomainName { get { return DataString; } set { DataString = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个A记录实例</summary>
        public DNS_PTR()
        {
            Type = DNSQueryType.PTR;
            Class = DNSQueryClass.IN;
        }
        #endregion
    }
}