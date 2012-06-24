using System;

namespace NewLife.Net.DNS
{
    /// <summary>SOA记录</summary>
    public class DNS_SOA : DNSRecord
    {
        #region 属性
        private String _PrimaryNameServer;
        /// <summary>主要名称服务器</summary>
        public String PrimaryNameServer { get { return _PrimaryNameServer; } set { _PrimaryNameServer = value; } }

        private String _ResponsibleAuthorityMail;
        /// <summary>认证邮箱</summary>
        public String ResponsibleAuthorityMail { get { return _ResponsibleAuthorityMail; } set { _ResponsibleAuthorityMail = value; } }

        private Int32 _SerialNumber;
        /// <summary>序列号</summary>
        public Int32 SerialNumber { get { return _SerialNumber; } set { _SerialNumber = value; } }

        private TimeSpan _RefreshInterval;
        /// <summary>刷新间隔</summary>
        public TimeSpan RefreshInterval { get { return _RefreshInterval; } set { _RefreshInterval = value; } }

        private TimeSpan _RetryInterval;
        /// <summary>重试间隔</summary>
        public TimeSpan RetryInterval { get { return _RetryInterval; } set { _RetryInterval = value; } }

        private TimeSpan _ExpirationLimit;
        /// <summary>过期限制</summary>
        public TimeSpan ExpirationLimit { get { return _ExpirationLimit; } set { _ExpirationLimit = value; } }

        private TimeSpan _MinimumTTL;
        /// <summary>最小TTL</summary>
        public TimeSpan MinimumTTL { get { return _MinimumTTL; } set { _MinimumTTL = value; } }

        /// <summary>文本信息</summary>
        public override String Text { get { return PrimaryNameServer; } set { PrimaryNameServer = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个SOA记录实例</summary>
        public DNS_SOA()
        {
            Type = DNSQueryType.SOA;
            Class = DNSQueryClass.IN;
        }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", Type, PrimaryNameServer);
        }
    }
}