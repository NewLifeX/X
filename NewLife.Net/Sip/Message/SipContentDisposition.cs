using System;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipContentDisposition : SipValueWithParams
    {
        #region 属性
        private String _DispositionType;
        /// <summary>DispositionType</summary>
        public String DispositionType { get { return _DispositionType; } set { _DispositionType = value; } }
        #endregion

        #region 扩展属性
        private String _Handling;
        /// <summary>handling</summary>
        public String Handling { get { return _Handling; } set { _Handling = value; } }
        #endregion
    }
}