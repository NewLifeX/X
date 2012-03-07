using System;

namespace NewLife.Net.DNS
{
    /// <summary>CNAME记录</summary>
    public class DNS_CNAME : DNSRecord
    {
        #region 属性
        private String _PrimaryName;
        /// <summary>IP地址</summary>
        public String PrimaryName { get { return _PrimaryName; } set { _PrimaryName = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个CNAME记录实例</summary>
        public DNS_CNAME()
        {
            Type = DNSQueryType.CNAME;
            Class = DNSQueryClass.IN;
        }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", Type, PrimaryName);
        }
    }
}