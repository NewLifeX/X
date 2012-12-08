using System;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipErrorUri : SipValueWithParams
    {
        #region 属性
        private String _Uri;
        /// <summary>属性说明</summary>
        public String Uri { get { return _Uri; } set { _Uri = value; } }
        #endregion

        #region 扩展属性
        #endregion
    }
}