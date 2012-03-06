using System;
using System.Net;

namespace NewLife.Net.DNS
{
    /// <summary>NS记录</summary>
    public class DNS_NS : DNSRecord
    {
        #region 属性
        /// <summary>命名服务器</summary>
        public String NameServer { get { return DataString; } set { DataString = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个NS记录实例</summary>
        public DNS_NS()
        {
            Type = DNSQueryType.NS;
            Class = DNSQueryClass.IN;
        }
        #endregion
    }
}