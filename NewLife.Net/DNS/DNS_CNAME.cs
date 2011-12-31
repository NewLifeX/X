using System;

namespace NewLife.Net.DNS
{
    /// <summary>CNAME记录</summary>
    public class DNS_CNAME : DNSBase<DNS_CNAME>
    {
        #region 属性
        /// <summary>IP地址</summary>
        public String PrimaryName { get { return DataString; } set { DataString = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个CNAME记录实例</summary>
        public DNS_CNAME()
        {
            Type = DNSQueryType.CNAME;
            Class = DNSQueryClass.IN;
        }
        #endregion
    }
}