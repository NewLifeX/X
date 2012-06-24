using System;

namespace NewLife.Net.DNS
{
    /// <summary>MX记录</summary>
    public class DNS_MX : DNSRecord
    {
        #region 属性
        private Int16 _Preference;
        /// <summary>引用</summary>
        public Int16 Preference { get { return _Preference; } set { _Preference = value; } }

        private String _Host;
        /// <summary>主机</summary>
        public String Host { get { return _Host; } set { _Host = value; } }

        /// <summary>文本信息</summary>
        public override String Text
        {
            get { return String.Format("{0},{1}", Preference, Host); }
            set
            {
                if (String.IsNullOrEmpty(value) || !value.Contains(","))
                {
                    Preference = 0;
                    Host = null;
                    return;
                }

                var p = value.IndexOf(",");
                Preference = Int16.Parse(value.Substring(0, p));
                Host = value.Substring(p + 1);
            }
        }
        #endregion

        #region 构造
        /// <summary>构造一个MX记录实例</summary>
        public DNS_MX()
        {
            Type = DNSQueryType.MX;
            Class = DNSQueryClass.IN;
        }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1} {2}", Type, Preference, Host);
        }
    }
}