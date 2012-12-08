using System;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipAlertParam : SipValueWithParams
    {
        #region 属性
        private String _Uri;
        /// <summary>标识</summary>
        public String Uri { get { return _Uri; } set { _Uri = value; } }
        #endregion
    }
}