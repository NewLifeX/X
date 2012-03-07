using System;

namespace NewLife.Net.DNS
{
    /// <summary>NS记录</summary>
    public class DNS_NS : DNSRecord
    {
        #region 属性
        private String _NameServer;
        /// <summary>命名服务器</summary>
        public String NameServer { get { return _NameServer; } set { _NameServer = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个NS记录实例</summary>
        public DNS_NS()
        {
            Type = DNSQueryType.NS;
            Class = DNSQueryClass.IN;
        }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", Type, NameServer);
        }
    }
}