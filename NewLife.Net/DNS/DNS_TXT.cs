using System;
using NewLife.Serialization;

namespace NewLife.Net.DNS
{
    /// <summary>TXT记录</summary>
    public class DNS_TXT : DNSRecord
    {
        #region 属性
        [FieldSize("_Length")]
        private String _Text;
        /// <summary>文本</summary>
        public String Text { get { return _Text; } set { _Text = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个TXT记录实例</summary>
        public DNS_TXT()
        {
            Type = DNSQueryType.TXT;
            Class = DNSQueryClass.IN;
        }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", Type, Text);
        }
    }
}