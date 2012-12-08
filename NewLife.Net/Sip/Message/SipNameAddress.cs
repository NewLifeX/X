using System;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipNameAddress
    {
        #region 属性
        private String _DisplayName;
        /// <summary>显示名</summary>
        public String DisplayName { get { return _DisplayName; } set { _DisplayName = value; } }

        private Uri _Uri;
        /// <summary>唯一标识</summary>
        public Uri Uri { get { return _Uri; } set { _Uri = value; } }
        #endregion
    }
}