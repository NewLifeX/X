using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipIdentityInfo : SipValueWithParams
    {
        #region 属性
        private String _Uri;
        /// <summary>属性说明</summary>
        public String Uri { get { return _Uri; } set { _Uri = value; } }
        #endregion

        #region 扩展属性
        private String _Alg;
        /// <summary>alg</summary>
        public String Alg { get { return _Alg; } set { _Alg = value; } }
        #endregion
    }
}